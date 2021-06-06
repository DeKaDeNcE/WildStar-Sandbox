// Copyright (c) Arctium.

using System;
using System.IO;
using System.Text;
using Framework.Misc;
using RealmServer.Constants.Net;

namespace RealmServer.Network.Packets
{
    class Packet
    {
        public PacketHeader Header { get; set; }
        public byte[] Data { get; set; }

        BinaryReader readStream;
        BinaryWriter writeStream;

        int shiftedBits = 0;
        byte packedByte = 0;

        public Packet()
        {
            writeStream = new BinaryWriter(new MemoryStream());
        }

        public Packet(ServerMessage message)
        {
            writeStream = new BinaryWriter(new MemoryStream());

            Header = new PacketHeader { Message = (ushort)message };

            Write(message, 12);

            FlushServer();
        }

        public Packet(byte[] data)
        {
            if (data.Length >= 4)
            {
                var offset = 0;

                readStream = new BinaryReader(new MemoryStream(data));

                Header = new PacketHeader
                {
                    Size = Read<uint>(32),
                    Message = Read<ushort>(12)
                };

                FlushClient();

                if (Header.Message == (ushort)ClientMessage.Composite)
                {
                    Header.Size = Read<uint>(24);

                    FlushClient();

                    offset = (int)readStream.BaseStream.Position + 1;

                    Data = new byte[Header.Size - 4];
                    Buffer.BlockCopy(data, offset, Data, 0, (int)Header.Size - 4);
                }
                else
                {
                    Data = new byte[Header.Size];
                    Buffer.BlockCopy(data, offset, Data, 0, (int)Header.Size);
                }
            }
        }

        public void FinishData()
        {
            if (shiftedBits > 0)
                FlushServer();

            Data = (writeStream.BaseStream as MemoryStream).ToArray();
        }

        public void Finish()
        {
            writeStream = new BinaryWriter(new MemoryStream());

            Write(Data.Length + 10, 24);

            FlushServer();

            Write(ServerMessage.GatewayToClientWrapper, 12);

            FlushServer();

            Write(Data.Length + 4, 24);

            FlushServer();

            writeStream.Write(Data);

            Data = (writeStream.BaseStream as MemoryStream).ToArray();
        }

        public void Write(object value, int bits)
        {
            if (value.GetType() == typeof(float))
            {
                WriteFloat((float)value, bits);
                return;
            }

            var val = value.ChangeType<ulong>();
            var bitsToWrite = 0;
            var writtenBits = 0;
            var count = bits;

            while (count > 0)
            {
                bitsToWrite = 8 - shiftedBits;

                if (bitsToWrite > bits - writtenBits)
                    bitsToWrite = bits - writtenBits;

                packedByte |= (byte)(((ulong)((1 << bitsToWrite) - 1) & val) << (shiftedBits & 0x1F));

                count -= bitsToWrite;

                writtenBits += bitsToWrite;

                shiftedBits = (bitsToWrite + shiftedBits) & 7;

                if (shiftedBits == 0)
                    FlushServer();

                val >>= bitsToWrite;
            }
        }

        public void WriteFloat(float value, int bits)
        {
            if (bits > 16)
                Write(BitConverter.ToUInt32(BitConverter.GetBytes(value), 0), bits);
            else
                Write(BitConverter.ToUInt16(BitConverter.GetBytes(value), 0), bits);
        }

        public void WriteWString(string value)
        {
            var data = Encoding.Unicode.GetBytes(value);
            var isLongString = (data.Length >> 1) > 0x7F;

            Write(isLongString, 1);
            Write(data.Length >> 1, isLongString ? 15 : 7);

            for (var i = 0; i < data.Length; i += 2)
                Write(BitConverter.ToUInt16(data, i), 16);
        }

        public void Write(byte[] data) => writeStream.Write(data);

        public void ReadMessage()
        {
            readStream = new BinaryReader(new MemoryStream(Data));

            Header.Message = Read<ushort>(12);

            FlushClient();
        }

        public T Read<T>(int bits = 0, string fieldName = "")
        {
            T value = default(T);
            if (bits == 0)
                value = readStream.Read<T>();
            else
            {

                var type = typeof(T);
                var finalType = type.IsEnum ? type.GetEnumUnderlyingType() : type;

                if (type == typeof(float) && bits <= 16)
                    value = (T)Convert.ChangeType(BitConverter.ToSingle(BitConverter.GetBytes(AddBits<ushort>(bits)).Combine(new byte[2]), 0), finalType);
                else if (type == typeof(float))
                    value = (T)Convert.ChangeType(BitConverter.ToSingle(BitConverter.GetBytes(AddBits<uint>(bits)), 0), finalType);
                else
                    value = (T)Convert.ChangeType(AddBits<T>(bits), finalType);
            }
            return value;
        }

        T AddBits<T>(int bits)
        {
            ulong unpackedValue = 0;
            packedByte = Read<byte>();
            var bitsToRead = 0;
            var readBitCT = 0;
            var count = 0;

            while (readBitCT < bits)
            {

                bitsToRead = 8 - shiftedBits;

                if (bitsToRead > bits - readBitCT)
                    bitsToRead = bits - readBitCT;

                unpackedValue |= (uint)((((1 << bitsToRead) - 1) & (packedByte >> (byte)shiftedBits)) << count);

                readBitCT = bitsToRead + count;
                count += bitsToRead;

                shiftedBits = (bitsToRead + shiftedBits) & 7;

                if (shiftedBits == 0 && readStream.BaseStream.Position < readStream.BaseStream.Length)
                    packedByte = Read<byte>();
            }

            readStream.BaseStream.Position -= 1;

            return (T)Convert.ChangeType(unpackedValue, typeof(T));
        }

        public byte[] Read(int count)
        {
            FlushClient();

            return readStream.ReadBytes(count);
        }

        public byte[] ReadByteArray(int count = -1)
        {
            if (count == -1)
                count = Read<int>(32);

            var ret = new byte[count];

            for (var i = 0; i < count; i++)
                ret[i] = Read<byte>(8);

            return ret;
        }

        public ushort[] ReadUShortArray(int count = -1)
        {
            if (count == -1)
                count = Read<int>(32);

            var ret = new ushort[count];

            for (var i = 0; i < count; i++)
                ret[i] = Read<ushort>(16);

            return ret;
        }

        public uint[] ReadUIntArray(int count = -1)
        {
            if (count == -1)
                count = Read<int>(32);

            var ret = new uint[count];

            for (var i = 0; i < count; i++)
                ret[i] = Read<uint>(32);

            return ret;
        }

        public ulong[] ReadULongArray(int count = -1)
        {
            if (count == -1)
                count = Read<int>(32);

            var ret = new ulong[count];

            for (var i = 0; i < count; i++)
                ret[i] = Read<ulong>(64);

            return ret;
        }

        public string ReadString()
        {
            var s = "";
            var length = Read<ushort>(16);

            for (int i = 1; i < length; i++)
            {
                var c = Convert.ToChar(Read<ushort>(16));

                if (c.ToString() != "\0")
                    s += c;
            }

            return s;
        }

        public string ReadWString()
        {
            var s = "";
            var isLongString = Read<bool>(1);
            var length = Read<ushort>(isLongString ? 15 : 7);

            for (int i = 0; i < length; i++)
            {
                var c = Convert.ToChar(Read<ushort>(16));

                if (c.ToString() != "\0")
                    s += c;
            }

            return s;
        }

        public void FlushClient()
        {
            if (shiftedBits != 0)
            {
                shiftedBits = 0;
                packedByte = Read<byte>();
            }
        }

        public void FlushServer()
        {
            writeStream.Write(packedByte);

            shiftedBits = 0;
            packedByte = 0;
        }

        public void FlushServer2()
        {
            if (shiftedBits != 0)
            {
                writeStream.Write(packedByte);

                shiftedBits = 0;
                packedByte = 0;
            }
        }

        public void ShiftReset()
        {
            //writeStream.Write(packedByte);

            shiftedBits = 0;
            packedByte = 0;
        }
    }
}
