// Copyright (c) Arctium.

using System;

namespace Framework.Misc
{
    public class Helper
    {
        public static byte[] GenerateRandomKey(int length)
        {
            var random = new Random((int)((uint)(Guid.NewGuid().GetHashCode() ^ 1 >> 89 << 2 ^ 42)).LeftRotate(13));
            var key = new byte[length];

            for (int i = 0; i < length; i++)
            {
                int randValue = -1;

                do
                {
                    randValue = (int)((uint)random.Next(0xFF)).LeftRotate(1) ^ i;
                } while (randValue > 0xFF && randValue <= 0);

                key[i] = (byte)randValue;
            }

            return key;
        }

        public static T Read<T>(string[] args, int index)
        {
            try
            {
                return args[index].ChangeType<T>();
            }
            catch
            {
                Console.WriteLine("Wrong arguments.");
            }

            return default(T);
        }
    }
}
