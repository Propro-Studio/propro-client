﻿/*
 * Copyright (c) 2020 CSi Studio
 * Aird and AirdPro are licensed under Mulan PSL v2.
 * You can use this software according to the terms and conditions of the Mulan PSL v2. 
 * You may obtain a copy of Mulan PSL v2 at:
 *          http://license.coscl.org.cn/MulanPSL2 
 * THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.  
 * See the Mulan PSL v2 for more details.
 */

using AirdPro.Constants;
using AirdPro.Domains.Aird;
using AirdPro.Utils;
using pwiz.CLI.cv;
using pwiz.CLI.msdata;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AirdPro.Domains.Convert;

namespace AirdPro.Converters
{
    internal class SWATH : IConverter
    {
        private double overlap;//SWATH窗口间的区域重叠值
        private int rangeSize;//总计的WindowRange数目,这里包含了ms1的一个窗口.例如SWATH窗口为32个,那么这边存储的数字就是33
        private int progress;//进度计数器
        private Hashtable rangeTable = new Hashtable(); //用于存放SWATH窗口的信息,key为mz
        public SWATH(JobInfo jobInfo) : base(jobInfo) {}

        public override void doConvert()
        {
            start();
            initDirectory();//创建文件夹
            using (airdStream = new FileStream(jobInfo.airdFilePath, FileMode.Create))
            {
                using (airdJsonStream = new FileStream(jobInfo.airdJsonFilePath, FileMode.Create))
                {
                    readVendorFile();//准备读取Vendor文件
                    buildWindowsRanges();  //Getting SWATH Windows
                    computeOverlap(); //Compute Overlap
                    adjustOverlap(); //Adjust Overlap
                    initGlobalVar();//初始化全局变量
                    preProgress();//预处理谱图,将MS1和MS2谱图分开存储
                    parseAndStoreMS1Block();
                    parseAndStoreMS2Block();
                    writeToAirdInfoFile();//将Info数据写入文件
                }
            }
            finish();
        }

        //初始化全局变量
        private void initGlobalVar()
        {
            rangeSize = ranges.Count + 1; //加上MS1的一个Range
            totalSize = msd.run.spectrumList.size(); //原始文件中谱图的总数目(包含MS1和MS2)
            startPosition = 0;//文件的存储位置,每一次解析完就会将指针往前挪移
            progress = 0;//扫描进度计数器
            //PreCheck size数目必须是rangeSize的整倍数,否则数据有误
            if (totalSize % rangeSize != 0) jobInfo.log("Spectrum not perfect,Total Size : " + totalSize + "|Range Size : " + rangeSize);
            jobInfo.log("Swath Windows Size:" + ranges.Count).log("Total Size(Include MS1):" + totalSize).log("Overlap:" + overlap);
            foreach (WindowRange range in ranges)
            {
                rangeTable.Add(range.mz, range);
            }
        }

        //解析MS1和MS2谱图
        protected void preProgress()
        {
            int parentNum = 0;
            jobInfo.log("Preprocessing:" + totalSize, "Preprocessing");
            int proprogress = 0;
            // 预处理所有的MS谱图
            for (int i = 0; i < totalSize; i++)
            {
                proprogress++;
                jobInfo.log(null, "Pre:" + proprogress + "/" + totalSize);
                Spectrum spectrum = spectrumList.spectrum(i);
                string msLevel = getMsLevel(spectrum);
                //如果这个谱图是MS1                          
                if (msLevel.Equals(MsLevel.MS1))
                {
                    parentNum = i;
                    ms1List.Add(parseMS1(spectrum, i));
                }
                //如果这个谱图是MS2
                if (msLevel.Equals(MsLevel.MS2))
                {
                    addToMS2Map(parseMS2(spectrum, i, parentNum));
                }
            }
            jobInfo.log("Effective MS1 List Size:" + ms1List.Count);
            jobInfo.log("MS2 Group List Size:" + ms2Table.Count);
            jobInfo.log("Start Processing MS1 List");
        }

       
        
        //提取SWATH 窗口信息
        private void buildWindowsRanges()
        {
            jobInfo.log("Start getting windows", "Getting Windows");
            int i = 0;
            Spectrum spectrum = spectrumList.spectrum(0);
            while (!spectrum.cvParamChild(CVID.MS_ms_level).value.ToString().Equals(MsLevel.MS1))
            {
                i++;
                spectrum = spectrumList.spectrum(i);
                if (i > spectrumList.size() / 2 || i > 500)
                {
                    //如果找了一半的spectrum或者找了超过500个spectrum仍然没有找到ms1,那么数据格式有问题,返回空;
                    jobInfo.logError("Search for more than 500 spectrums and no ms1 was found. File Format Error");
                    throw new Exception("Search for more than 500 spectrums and no ms1 was found. File Format Error");
                }
            }

            i++;
            spectrum = spectrumList.spectrum(i);
            while (spectrum.cvParamChild(CVID.MS_ms_level).value.ToString().Equals(MsLevel.MS2))
            {
                double mz, lowerOffset, upperOffset;
                mz = getPrecursorIsolationWindowParams(spectrum, CVID.MS_isolation_window_target_m_z);
                lowerOffset = getPrecursorIsolationWindowParams(spectrum, CVID.MS_isolation_window_lower_offset);
                upperOffset = getPrecursorIsolationWindowParams(spectrum, CVID.MS_isolation_window_upper_offset);
                
                WindowRange range = new WindowRange(mz - lowerOffset, mz + upperOffset, mz);
                Hashtable features = new Hashtable();
                features.Add(Features.original_width, lowerOffset + upperOffset);
                features.Add(Features.original_precursor_mz_start, mz - lowerOffset);
                features.Add(Features.original_precursor_mz_end, mz + upperOffset);
                range.features = FeaturesUtil.toString(features);
                ranges.Add(range);

                i++;
                spectrum = spectrumList.spectrum(i);
            }
            jobInfo.log("Finished Getting Windows");
        }

        //计算窗口间的重叠区域的大小
        private void computeOverlap()
        {
            WindowRange range1 = ranges[0];
            double range1Right = range1.end;
            WindowRange range2 = ranges[1];
            double range2Left = range2.start;
            overlap = range1Right - range2Left;
            featuresMap.Add(Features.overlap, overlap);
        }

        //调整间距,第一个窗口的上区间不做调整,最后一个窗口的下区间不做调整
        private void adjustOverlap()
        {
            int size = ranges.Count;
            if (size <= 2)
            {
                jobInfo.log("Windows Size Exception: Only " + size + " Windows");
                throw new Exception("Windows Size Exception: Only " + size + " Windows");
            }
            ranges[0].end = ranges[0].end - (overlap / 2);  //第一个窗口的上区间保持不变,下区间做调整
            ranges[size - 1].start = ranges[size - 1].start + (overlap / 2); //最后一个窗口的下区间不变,上区间做调整

            for (int i=1;i < size - 1;i++)
            {
                ranges[i].start = ranges[i].start + (overlap / 2);
                ranges[i].end = ranges[i].end - (overlap / 2);
            }
        }

        private void parseAndStoreMS2Block()
        {
            jobInfo.log("Start Processing MS2 List");
            foreach (double key in ms2Table.Keys)
            {
                List<TempIndex> tempIndexList = ms2Table[key] as List<TempIndex>;
                WindowRange range = rangeTable[key] as WindowRange;
                //为每一个key组创建一个SwathBlock
                BlockIndex index = new BlockIndex();
                index.level = 2;
                index.startPtr = startPosition;
                index.setWindowRange(range);

                jobInfo.log(null, "MS2:" + progress + "/" + ms2Table.Keys.Count);
                progress++;

                if (jobInfo.jobParams.threadAccelerate)
                {
                    Hashtable table = Hashtable.Synchronized(new Hashtable());
                    //使用多线程处理数据提取与压缩
                    Parallel.For(0, tempIndexList.Count, (i, ParallelLoopState) =>
                    {
                        TempIndex tempIndex = tempIndexList[i];
                        TempScan ts = new TempScan(tempIndex.num, tempIndex.rt);
                        compress(spectrumList.spectrum(tempIndex.num, true), ts);
                        //SwathIndex中只存储MS2谱图对应的MS1谱图的序号,其本身的序号已经没用了,不做存储,所以只存储了pNum
                        table.Add(i, ts);
                    });

                    outputWithOrder(table, index);
                }
                else
                {
                    foreach (TempIndex tempIndex in tempIndexList)
                    {
                        TempScan ts = new TempScan(tempIndex.pNum, tempIndex.rt);
                        compress(spectrumList.spectrum(tempIndex.num, true), ts);
                        //SwathIndex中只存储MS2谱图对应的MS1谱图的序号,其本身的序号已经没用了,不做存储
                        addToIndex(index, ts);
                    }
                }

                index.endPtr = startPosition;
                indexList.Add(index);
                jobInfo.log("MS2 Group Finished:" + progress + "/" + ms2Table.Keys.Count);
            }
        }
    }
}