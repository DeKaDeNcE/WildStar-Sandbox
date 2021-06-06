// Copyright (c) Arctium.

namespace RealmServer.Managers
{
    public class SniffPacket
    {
        public bool Direction;
        public double Timestamp;
        public int MessageId;
        public byte[] Data;
        public bool Self;
    }
}