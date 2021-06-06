// Copyright (c) Arctium.

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Framework.Misc
{
    public static class Extensions
    {
        static Dictionary<Type, Func<BinaryReader, object>> ReadValue = new Dictionary<Type, Func<BinaryReader, object>>()
        {
            { typeof(bool),      br => br.ReadBoolean() },
            { typeof(sbyte),     br => br.ReadSByte()   },
            { typeof(byte),      br => br.ReadByte()    },
            { typeof(char),      br => br.ReadChar()    },
            { typeof(short),     br => br.ReadInt16()   },
            { typeof(ushort),    br => br.ReadUInt16()  },
            { typeof(int),       br => br.ReadInt32()   },
            { typeof(uint),      br => br.ReadUInt32()  },
            { typeof(float),     br => br.ReadSingle()  },
            { typeof(long),      br => br.ReadInt64()   },
            { typeof(ulong),     br => br.ReadUInt64()  },
            { typeof(double),    br => br.ReadDouble()  },
        };

        interface UnitInfo { }

        struct NonPlayer : UnitInfo { }

        static Dictionary<uint, UnitInfo> ReadUnitInfo = new Dictionary<uint, UnitInfo>()
        {
            { 0,  new NonPlayer() },
        };



        public static T Read<T>(this BinaryReader br)
        {
            var type = typeof(T);
            var finalType = type.IsEnum ? type.GetEnumUnderlyingType() : type;

            return (T)ReadValue[finalType](br);
        }

        public static uint LeftRotate(this uint value, int shiftCount)
        {
            return (value << shiftCount) | (value >> (0x20 - shiftCount));
        }

        public static byte[] ToByteArray(this string s)
        {
            var data = new byte[s.Length / 2];

            for (int i = 0; i < s.Length; i += 2)
                data[i / 2] = Convert.ToByte(s.Substring(i, 2), 16);

            return data;
        }

        public static bool Compare(this byte[] b, byte[] b2)
        {
            for (int i = 0; i < b2.Length; i++)
                if (b[i] != b2[i])
                    return false;

            return true;
        }

        public static byte[] Combine(this byte[] data, params byte[][] pData)
        {
            var combined = data;

            foreach (var arr in pData)
            {
                var currentSize = combined.Length;

                Array.Resize(ref combined, currentSize + arr.Length);

                Buffer.BlockCopy(arr, 0, combined, currentSize, arr.Length);
            }

            return combined;
        }

        public static string ToHexString(this byte[] data)
        {
            var hex = "";

            foreach (var b in data)
                hex += $"{b:X2}";

            return hex.ToUpper();
        }

        public static byte[] ReverseUInt32(this byte[] data)
        {
            var ret = new byte[data.Length];

            for (var i = 0; i < data.Length; i += 4)
                Buffer.BlockCopy(data, i, ret, ret.Length - (i + 4), 4);

            return ret;
        }

        public static byte[] Fill32Bits(this byte[] data)
        {
            return data.Combine(new byte[4 - (data.Length % 4)]);
        }

        public static BigInteger ToBigInteger<T>(this T value, bool isBigEndian = false)
        {
            var ret = BigInteger.Zero;

            switch (typeof(T).Name)
            {
                case "Byte[]":
                    var data = value as byte[];

                    if (isBigEndian)
                        Array.Reverse(data);

                    ret = new BigInteger(data.Combine(new byte[] { 0 }));
                    break;
                case "BigInteger":
                    ret = (BigInteger)Convert.ChangeType(value, typeof(BigInteger));
                    break;
                default:
                    throw new NotSupportedException(string.Format("'{0}' conversion to 'BigInteger' not supported.", typeof(T).Name));
            }

            return ret;
        }

        public static byte[] ToArray(this BinaryWriter br)
        {
            return (br.BaseStream as MemoryStream).ToArray();
        }

        public static T ChangeType<T>(this object value)
        {
            return (T)ChangeType(value, typeof(T));
        }

        public static object ChangeType(this object value, Type destType)
        {
            return destType.IsEnum ? Enum.ToObject(destType, value) : Convert.ChangeType(value, destType);
        }
    }
}
