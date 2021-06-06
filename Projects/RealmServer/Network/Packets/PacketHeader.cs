// Copyright (c) Arctium.

namespace RealmServer.Network.Packets
{
    class PacketHeader
    {
        public ushort Message { get; set; }
        public uint Size      { get; set; }
    }
}
