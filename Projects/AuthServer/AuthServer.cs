// Copyright (c) Arctium.

using System;
using System.Globalization;
using System.Threading;
using AuthServer.Network;
using AuthServer.Network.Packets;
using Framework.Logging;

namespace AuthServer
{
    public class AuthServer
    {
        public void Run() => Main(null);
        void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            PacketLog.Initialize("Logs/AuthServer", "Packets.log");

            using (var server = new Server("0.0.0.0", 23115))
            {
                PacketManager.DefineMessageHandler();

                Console.WriteLine("AUTHSERVER!!! :)");

                while (true)
                    Thread.Sleep(1);
            }
        }
    }
}
