﻿namespace Propro_Client.Constants
{
    class SoftwareVersion
    {
        public static int AIRD_VERSION = 2;
        public static string CLIENT_VERSION = "1.9.1";
        public static string CLIENT_VERSION_DESCRIPTION = "New Function: Support for Scanning Swath";

        public static string getVersion()
        {
            return "Propro-Client V" + SoftwareVersion.CLIENT_VERSION + " (Aird Version Code:" + SoftwareVersion.AIRD_VERSION + ")";
        }

        public static string getDescription()
        {
            return CLIENT_VERSION_DESCRIPTION;
        }
    }
}
