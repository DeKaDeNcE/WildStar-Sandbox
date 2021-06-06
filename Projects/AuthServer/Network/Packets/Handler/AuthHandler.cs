// Copyright (c) Arctium.

using System;
using AuthServer.Attributes;
using AuthServer.Constants.Net;
using Framework.Misc;

namespace AuthServer.Network.Packets.Handler
{
    class AuthHandler
    {
        [AuthPacket(ClientMessage.State1)]
        public static void HandleState1(Packet packet, AuthSession session)
        {
            // Send same data back for now.
            session.SendRaw(packet.Data);
        }

        [AuthPacket(ClientMessage.State2)]
        public static void HandleState2(Packet packet, AuthSession session)
        {
            // Send same data back for now.
            session.SendRaw(packet.Data);
        }

        [AuthPacket(ClientMessage.AuthRequest)]
        public static void HandleAuthRequest(Packet packet, AuthSession session)
        {
            packet.Read<uint>(32);
            packet.Read<ulong>(64);

            var loginName = packet.ReadString();

            Console.WriteLine($"Account '{loginName}' tries to connect.");

            {
                var authComplete = new Packet(ServerMessage.AuthComplete);

                authComplete.Write(0, 32);

                session.Send(authComplete);

                var realmMessage = new Packet(ServerMessage.RealmMessage);
                realmMessage.Write(3, 32);
                realmMessage.Write(0, 32);

                // linecounts
                realmMessage.Write(6, 8);

                realmMessage.WriteWString("Welcome to Arctium - WildStar Sandbox!");
                realmMessage.WriteWString("Welcome to Arctium - WildStar Sandbox");
                realmMessage.WriteWString("Welcome to Arctium - WildStar Sandbox!");
                realmMessage.WriteWString("");
                realmMessage.WriteWString("");
                realmMessage.WriteWString("");

                // No wrapper here?!
                //session.Send(realmMessage);

                var connectToRealm = new Packet(ServerMessage.ConnectToRealm);
                var ip = BitConverter.ToUInt32(new byte[] { 1, 0, 0, 127 }, 0);
                connectToRealm.Write(ip, 32);
                connectToRealm.Write(24000, 16);
                connectToRealm.Write(Helper.GenerateRandomKey(16)); // gatewayTicket
                connectToRealm.Write(0, 32);
                connectToRealm.WriteWString("");
                connectToRealm.Write(0, 32);
                connectToRealm.Write(0, 2);
                connectToRealm.Write(0, 21);

                session.Send(connectToRealm);
            }
        }
    }
}
