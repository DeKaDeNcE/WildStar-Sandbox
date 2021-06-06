// Copyright (c) Arctium.


namespace StsServer.Configuration
{
    class StsConfig
    {
        #region Config Options
        public static string LogDirectory;
        public static string LogConsoleFile;
        public static string LogPacketFile;

        public static string BindIP;
        public static int BindPort;
        #endregion

        public static void Initialize()
        {
            LogDirectory = "Logs/StsServer";
            LogConsoleFile = "Console.log";
            LogPacketFile = "Packet.log";

            BindIP = "0.0.0.0";
            BindPort = 6600;
        }
    }
}
