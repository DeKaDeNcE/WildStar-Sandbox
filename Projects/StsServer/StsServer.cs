// Copyright (c) Arctium.

using System;
using StsServer.Configuration;
using StsServer.Network;
using StsServer.Network.Packets;
using System.Threading;
using System.Text;
using System.Globalization;

namespace StsServer
{
    class StsServer
    {
        public static Server server;
        static void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            StsConfig.Initialize();

            var serverName = "WildStar Sandbox";

            Console.ForegroundColor = ConsoleColor.Cyan;

            Console.WriteLine("_________________WildStar________________");
            Console.WriteLine("                   _   _                 ");
            Console.WriteLine(@"    /\            | | (_)                ");
            Console.WriteLine(@"   /  \   _ __ ___| |_ _ _   _ _ __ ___  ");
            Console.WriteLine(@"  / /\ \ | '__/ __| __| | | | | '_ ` _ \ ");
            Console.WriteLine(@" / ____ \| | | (__| |_| | |_| | | | | | |");
            Console.WriteLine(@"/_/    \_\_|  \___|\__|_|\__,_|_| |_| |_|");
            Console.WriteLine();

            var sb = new StringBuilder();

            sb.Append("_________________________________________");

            var nameStart = (42 - serverName.Length) / 2;

            sb.Insert(nameStart, serverName);
            sb.Remove(nameStart + serverName.Length, serverName.Length);

            Console.WriteLine(sb.ToString());


            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();

            Console.WriteLine($"Starting {serverName}...");

            using (server = new Server(StsConfig.BindIP, StsConfig.BindPort))
            {
                server.accept = true;
                StsPacketManager.DefineMessageHandler();

                new Thread(() => new AuthServer.AuthServer().Run()).Start();
                new Thread(() => new RealmServer.RealmServer().Run()).Start();

                Console.WriteLine("Done.");

                while (true)
                {
                    Thread.Sleep(1);
                }
                //
                // Prevents auto close.
                //ConsoleCommandManager.InitCommands();
            }
        }
    }
}
