﻿namespace Propro_Client.Domains.Aird
{
    public class Features
    {
        //原始文件中前体的窗口大小,未经过overlap参数调整,precursor mz
        public static string original_width = "owid";
        //原始文件中前体的荷质比窗口开始位置,未经过overlap参数调整,precursor mz
        public static string original_precursor_mz_start = "oMzStart";
        //原始文件中前体的荷质比窗口结束位置,未经过overlap参数调整,precursor mz
        public static string original_precursor_mz_end = "oMzEnd";

        //从Vendor文件中得到的msd.id
        public static string raw_id = "rawId";
        //是否忽略Intensity为0的键值对,默认为true
        public static string ignore_zero_intensity = "ignoreZeroIntensity";
        //SWATH的各个窗口间的重叠部分
        public static string overlap = "overlap";
        //源文件数据格式
        public static string source_file_format = "sourceFileFormat";
        //Aird文件版本码
        public static string aird_version = "aird_version";
        //Propro Client软件版本号
        public static string propro_client_version = "propro_client_version";
        //进行zlib压缩时使用的byteOrder编码,C#默认使用的是LITTLE_ENDIAN
        public static string byte_order = "byte_order";

    }
}