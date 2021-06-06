// Copyright (c) Arctium.

namespace AuthServer.Network.Packets
{
    class PacketHeader
    {
        public ushort Message { get; set; }
        public uint Size      { get; set; }
    }
}
