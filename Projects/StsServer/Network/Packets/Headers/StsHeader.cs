// Copyright (c) Arctium.

using StsServer.Constants.Net;

namespace StsServer.Network.Packets.Headers
{
    class StsHeader
    {
        public StsMessage Message { get; set; }
        public ushort Length       { get; set; }
        public ushort DataLength   { get; set; }
        public byte Sequence       { get; set; }
    }
}
