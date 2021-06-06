// Copyright (c) Arctium.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using AuthServer.Attributes;
using AuthServer.Constants.Net;

namespace AuthServer.Network.Packets
{
    class PacketManager
    {
        static readonly ConcurrentDictionary<ClientMessage, HandlePacket> ClientMessageHandlers = new ConcurrentDictionary<ClientMessage, HandlePacket>();
        delegate void HandlePacket(Packet packet, AuthSession client);

        public static void DefineMessageHandler()
        {
            var currentAsm = Assembly.GetExecutingAssembly();

            foreach (var type in currentAsm.GetTypes())
                foreach (var methodInfo in type.GetMethods())
                    foreach (var msgAttr in methodInfo.GetCustomAttributes<AuthPacketAttribute>())
                        ClientMessageHandlers.TryAdd(msgAttr.Message, Delegate.CreateDelegate(typeof(HandlePacket), methodInfo) as HandlePacket);
        }

        public static bool Invoke(Packet reader, AuthSession session)
        {
            var message = (ClientMessage)reader.Header.Message;

            Console.WriteLine($"Received Auth packet: {message} (0x{message:X}), Length: {reader.Data.Length}");

            HandlePacket packet;

            if (ClientMessageHandlers.TryGetValue(message, out packet))
            {
                packet.Invoke(reader, session);

                return true;
            }

            return false;
        }
    }
}
