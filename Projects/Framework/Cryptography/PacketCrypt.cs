// Copyright (c) Arctium.

using System;

namespace Framework.Cryptography
{
    public class PacketCrypt
    {
        public bool Initialized { get; set; }

        ulong keyValue1 = 0x718DA9074F2DEB91;
        uint keyValue2 = 0xAA7F8EA9;
        ulong keyState;

        byte[] key = new byte[128];

        public PacketCrypt(byte[] gatewayTicket = null)
        {
            var baseKey = 0ul;

            if (gatewayTicket == null)
                baseKey = CalculateBaseKey();
            else
                baseKey = CalculateGatewayKey(gatewayTicket);

            keyState = keyValue1;

            var keyPart = keyValue2 * (baseKey + keyValue1);

            for (var i = 0; i < key.Length; i += 8)
            {
                var bytes = BitConverter.GetBytes(keyPart);

                Buffer.BlockCopy(bytes, 0, key, i, bytes.Length);

                keyPart = keyValue2 * (baseKey + keyPart);
            }

            for (var i = 32; i < key.Length + 32; i += 32)
            {
                keyState = keyValue2 * (BitConverter.ToUInt64(key, i - 32) + keyState);
                keyState = keyValue2 * (BitConverter.ToUInt64(key, i - 24) + keyState);
                keyState = keyValue2 * (BitConverter.ToUInt64(key, i - 16) + keyState);
                keyState = keyValue2 * (BitConverter.ToUInt64(key, i - 8) + keyState);
            }
        }

        ulong CalculateBaseKey()
        {
            return keyValue1 * (keyValue2 * (keyValue1 + 0x5B88D61139619662) + 16042) + 0x64F77F4AB4E81A0;
        }

        ulong CalculateGatewayKey(byte[] gatewayTicket)
        {
            var baseKey = CalculateBaseKey();
            var gatewayKey = keyValue1;

            for (var i = 0; i < gatewayTicket.Length; i++)
                gatewayKey = keyValue2 * (gatewayKey + gatewayTicket[i]);

            return keyValue2 * (gatewayKey + baseKey);
        }

        public void Encrypt(byte[] data, int length)
        {
            var state = BitConverter.GetBytes(keyState);
            var kIndex = 0xAA7F8EAA * length;
            var tmp = 0u;

            for (var i = 0; i < length; i++)
            {
                var index = i & 7;

                if (index == 0)
                    tmp = (uint)(kIndex++ & 0xF) << 3;

                data[i] = (byte)(state[index] ^ data[i] ^ key[index + tmp]);

                state[index] = data[i];
            }
        }

        public void Decrypt(byte[] data, int length)
        {
            var state = BitConverter.GetBytes(keyState);
            var kIndex = 0xAA7F8EAA * length;
            var tmp = 0u;

            for (var i = 0; i < length; i++)
            {
                var index = i & 7;

                if (index == 0)
                    tmp = (uint)(kIndex++ & 0xF) << 3;

                var unchangedByte = data[i];

                data[i] = (byte)(state[index] ^ data[i] ^ key[index + tmp]);

                state[index] = unchangedByte;
            }
        }
    }
}
