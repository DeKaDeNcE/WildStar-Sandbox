// Copyright (c) Arctium.

using System.Globalization;
using System.Threading;
using Framework.Logging;
using RealmServer.Managers;
using RealmServer.Network;
using RealmServer.Network.Packets;

namespace RealmServer
{
    public class RealmServer
    {
        public void Run() => Main(null);
        void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            PacketLog.Initialize("Logs/RealmServer", "Packets.log");

            using (var server = new Server("0.0.0.0", 24000))
            {
                PacketManager.DefineMessageHandler();
                Manager.Initialize();

                System.Console.ForegroundColor = System.ConsoleColor.Green;
                System.Console.WriteLine("You can login now :)");
                System.Console.WriteLine("user: arctium@arctium");
                System.Console.WriteLine("password: arctium");
                System.Console.ForegroundColor = System.ConsoleColor.White;

                while (true)
                    Thread.Sleep(1);
            }
        }
    }
}
