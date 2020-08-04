﻿namespace AirdPro.Constants
{
    class SoftwareVersion
    {
        public static int AIRD_VERSION = 5;
        public static string CLIENT_VERSION = "1.0.0";
        public static string CLIENT_VERSION_DESCRIPTION = "正式命名为AirdPro,合并SWATHIndex与BlockIndex为BlockIndex";

        public static string getVersion()
        {
            return "AirdPro V" + SoftwareVersion.CLIENT_VERSION + " (Aird Version Code:" + SoftwareVersion.AIRD_VERSION + ")";
        }

        public static string getDescription()
        {
            return CLIENT_VERSION_DESCRIPTION;
        }
    }
}
