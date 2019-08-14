﻿using Propro.Structs;
using Propro_Client.Domains.Aird;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Propro.Domains
{
    public class AirdInfo
    {
        /**
         * 仪器设备信息
         */
        public Instrument instrument;
    
        /**
         * 处理的软件信息
         */
        public List<Software> softwares;

        /**
         * 处理前的文件信息
         */
        public List<ParentFile> parentFiles;

        /**
         * [核心字段]
         * 数组压缩策略
         */
        public List<Compressor> compressors;

        /**
        * [核心字段]
        * Store the window rangs which have been adjusted with experiment overlap
        * 存储SWATH窗口信息,窗口已经根据overlap进行过调整
        */
        public List<WindowRange> rangeList;

        /**
         * [核心字段]
         * 用于存储SWATH Block的索引
         */
        public List<SwathIndex> indexList;

        /**
         * [核心字段]
         * 实验类型,目前支持DIA_SWATH,PRM和SCANNING_SWATH三种
         */
        public string type;

        /**
         * 转换压缩后的aird二进制文件路径,默认读取同目录下的同名文件,如果不存在才去去读本字段对应的路径
         */
        public string airdPath;

        /**
         * 原始文件的文件大小,单位byte
         */
        public long fileSize;

        /**
         * 实验的创建者,本字段在被导入Propro Server时会被操作人覆盖
         */
        public string creator;

        /**
         * 实验的创建日期
         */
        public DateTime createDate;

        /**
         * 特征键值对,详情见Features.cs
         */
        public string features;
       
    }
}
