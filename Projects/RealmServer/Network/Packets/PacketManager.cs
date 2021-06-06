// Copyright (c) Arctium.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using RealmServer.Attributes;
using RealmServer.Constants.Net;

namespace RealmServer.Network.Packets
{
    class PacketManager
    {
        static readonly ConcurrentDictionary<ClientMessage, HandlePacket> ClientMessageHandlers = new ConcurrentDictionary<ClientMessage, HandlePacket>();
        delegate void HandlePacket(Packet packet, RealmSession client);

        public static bool ReplayEnabled { get; internal set; }

        public static void DefineMessageHandler()
        {
            var currentAsm = Assembly.GetExecutingAssembly();

            foreach (var type in currentAsm.GetTypes())
                foreach (var methodInfo in type.GetMethods())
                    foreach (var msgAttr in methodInfo.GetCustomAttributes<RealmPacketAttribute>())
                        ClientMessageHandlers.TryAdd(msgAttr.Message, Delegate.CreateDelegate(typeof(HandlePacket), methodInfo) as HandlePacket);
        }

        public static bool Invoke(Packet reader, RealmSession session)
        {

            if (ReplayEnabled)
            {
                //Console.WriteLine("Opcode: {0} (0x{1:X}), Length: {2}", (ClientMessage)reader.Header.Message, reader.Header.Message, reader.Header.Size);

                // Only handle defined packets while replaying a sniff.
                switch ((ClientMessage)reader.Header.Message)
                {
                    case ClientMessage.RetrieveCharacterList:
                    case ClientMessage.PlayerLogin:
                    case ClientMessage.ChatMessage:
                    case ClientMessage.LogoutRequest:
                    case ClientMessage.UpdateCommand:
                        break;
                    default:
                        return false;
                }
            }

            var message = (ClientMessage)reader.Header.Message;

#if DEBUG
            Console.WriteLine($"Received Realm packet: {message} (0x{message:X}), Length: {reader.Data.Length}");
#endif
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
