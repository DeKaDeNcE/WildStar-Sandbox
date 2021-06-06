// Copyright (c) Arctium.

using System;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Json;

namespace Framework.Serialization
{

    public class Json
    {
        public static string CreateString<T>(T dataObject)
        {
            return Encoding.UTF8.GetString(CreateArray(dataObject));
        }

        public static byte[] CreateArray<T>(T dataObject)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            var stream = new MemoryStream();

            serializer.WriteObject(stream, dataObject);

            return stream.ToArray();
        }

        public static T CreateObject<T>(string jsonData)
        {
            return CreateObject<T>(Encoding.UTF8.GetBytes(jsonData));
        }

        public static T CreateObject<T>(byte[] jsonData) => (T)CreateObject<T>(new MemoryStream(jsonData));

        public static T CreateObject<T>(Stream jsonData)
        {
            if (jsonData.Length == 0)
                return default(T);

            var serializer = new DataContractJsonSerializer(typeof(T));

            try
            {
                return (T)serializer.ReadObject(jsonData);
            }
            catch (Exception)
            {
                return default(T);
            }
        }
    }
}
