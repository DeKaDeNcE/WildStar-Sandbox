// Copyright (c) Arctium.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using StsServer.Network.Packets;
using Framework.Cryptography;
using Framework.Cryptography.BNet;
using Framework.Logging;
using Framework.Misc;

namespace StsServer.Network
{
    class StsSession : IDisposable
    {
        public readonly string PasswordVerifier = "798FE128207A9040953C500813CE5ACFBA07BEC4B0438ED06D9AD537FEA247AAF119517FCE87E9E3574B09BFF02A3C27A463C77E675F95BCA2DFB91E00342722C11FC2BD63B2ECAC027B1110ECB5E1317A9CEAB3B784D3AEAAFB09D6C77D525C331D50A54F5B19FBF70231A13281B8679F1391FAFBCF048B989AD2C9DE8DBD17";
        public readonly string Salt = "9A39481FB4694263"; 
        public int State { get; set; }
        public SRP6a SecureRemotePassword { get; set; }
        public SARC4 ClientCrypt { get; set; }
        public SARC4 ServerCrypt { get; set; }

        Socket client;
        Stack<StsPacket> packetQueue;
        byte[] dataBuffer = new byte[0x400];

        public StsSession(Socket clientSocket)
        {
            client = clientSocket;
            packetQueue = new Stack<StsPacket>();
        }

        public void Accept()
        {
            var socketEventArgs = new SocketAsyncEventArgs();

            socketEventArgs.SetBuffer(dataBuffer, 0, dataBuffer.Length);

            socketEventArgs.Completed += OnConnection;
            socketEventArgs.UserToken = client;
            socketEventArgs.SocketFlags = SocketFlags.None;

            if (!client.ReceiveAsync(socketEventArgs))
                OnConnection(null, socketEventArgs);
        }

        public void OnConnection(object sender, SocketAsyncEventArgs e)
        {
            PacketLog.Write<StsPacket>(dataBuffer, e.BytesTransferred, client.RemoteEndPoint as IPEndPoint);

            if (e.BytesTransferred < 256 || e.BytesTransferred > 400)
            {
                Console.WriteLine("Wrong initialization packet.");
                //Console.WriteLine("NEW packet:");
                 var packetData = new byte[e.BytesTransferred];

                Buffer.BlockCopy(dataBuffer, 0, packetData, 0, e.BytesTransferred);
                Console.WriteLine(Encoding.UTF8.GetString(packetData));
                //Dispose();
            }
            //else
            {
                // POST
                if (BitConverter.ToUInt32(dataBuffer, 0) == 0x54534F50)
                {
                    var packetData = new byte[e.BytesTransferred];

                    Buffer.BlockCopy(dataBuffer, 0, packetData, 0, e.BytesTransferred);

                    var packetInfo = GetMessageType(packetData);

                    // Connect & PageVerifiedIps
                    if (packetInfo.Item1 != "Sts" && packetInfo.Item1 != "Auth")
                    {
                        Console.WriteLine("Wrong packet type for first packet.");

                        Dispose();

                        return;
                    }

                    var packet = new StsPacket(packetData);

                    packet.ReadHeader(packetInfo);

                    packetData = new byte[packet.Header.DataLength];

                    Buffer.BlockCopy(packet.Data, packet.Header.Length, packetData, 0, packet.Header.DataLength);

                    packet.Data.Combine(packetData);

                    try
                    {
                        packet.ReadData(packetData);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Packet reading failed. Ignoring that...");
                    }

                    StsPacketManager.Invoke(packet, this);

                    e.Completed -= OnConnection;
                    e.Completed += Process;

                    if (!client.ReceiveAsync(e))
                        Process(sender, e);
                }
            }
        }

        public void Process(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                var socket = e.UserToken as Socket;
                var receivedBytes = e.BytesTransferred;

                if (receivedBytes > 0)
                {
                    byte[] packetData = null;

                    if (packetQueue.Count > 0)
                    {
                        var lastPacket = packetQueue.Pop();

                        if (receivedBytes != lastPacket.Header.DataLength)
                        {
                            Console.WriteLine("Wrong data received. Ignoring that...");
                            Console.WriteLine(Encoding.UTF8.GetString(lastPacket.Data));
                            Console.WriteLine("NEW packet:");
                            packetData = new byte[receivedBytes];

                            Buffer.BlockCopy(dataBuffer, 0, packetData, 0, receivedBytes);
                            Console.WriteLine(Encoding.UTF8.GetString(packetData));
                            //Dispose();

                            //return;
                        }
                        else
                        {
                            packetData = new byte[receivedBytes];

                            Buffer.BlockCopy(dataBuffer, 0, packetData, 0, receivedBytes);

                            lastPacket.Data.Combine(packetData);
                            lastPacket.ReadData(packetData);

                            ProcessPacket(lastPacket);
                        }
                    }

                    if (ClientCrypt != null)
                    {
                        if (receivedBytes <= 100)
                            State = 0;

                        if (State == 0)
                            ClientCrypt.ProcessBuffer(dataBuffer, receivedBytes);
                    }

                    packetData = new byte[receivedBytes];

                    Buffer.BlockCopy(dataBuffer, 0, packetData, 0, receivedBytes);

                    // POST
                    if (BitConverter.ToUInt32(packetData, 0) == 0x54534F50)
                    {
                        var packet = new StsPacket(packetData);
                        var packetInfo = GetMessageType(packetData);
                        
                        packet.ReadHeader(packetInfo);

                        if ((receivedBytes - packet.Header.Length) != packet.Header.DataLength)
                            packetQueue.Push(packet);
                        else
                        {
                            receivedBytes -= packet.Header.Length;

                            packetData = new byte[receivedBytes];

                            Buffer.BlockCopy(packet.Data, packet.Header.Length, packetData, 0, receivedBytes);

                            if (packetData.Length > 0)
                                packet.ReadData(packetData);

                            ProcessPacket(packet);
                        }
                    }

                    if (!client.ReceiveAsync(e))
                        Process(sender, e);
                }
                else
                    Dispose();
            }
            catch
            {
                Dispose();
            }
        }

        public void ProcessPacket(StsPacket packet)
        {
            PacketLog.Write<StsPacket>(packet.Data, packet.Data.Length, client.RemoteEndPoint as IPEndPoint);

            // Default answer?!
            if (!StsPacketManager.Invoke(packet, this))
            {
            }
        }

        public void Send(StsPacket packet, bool noData = false)
        {
            try
            {
                packet.Finish(noData);

                PacketLog.Write<StsPacket>(packet.Data, packet.Data.Length, client.RemoteEndPoint as IPEndPoint);

                if (ServerCrypt != null)
                    ServerCrypt.ProcessBuffer(packet.Data);

                var socketEventargs = new SocketAsyncEventArgs();

                socketEventargs.SetBuffer(packet.Data, 0, packet.Data.Length);

                socketEventargs.Completed += SendCompleted;
                socketEventargs.UserToken = packet;
                socketEventargs.RemoteEndPoint = client.RemoteEndPoint;
                socketEventargs.SocketFlags = SocketFlags.None;

                client.SendAsync(socketEventargs);
            }
            catch
            {
                Dispose();
            }
        }

        void SendCompleted(object sender, SocketAsyncEventArgs e)
        {
        }

        public Tuple<string[], int> ReadLines(BinaryReader readStream)
        {
            var position = 0;

            while (position == 0 && readStream.BaseStream.CanRead)
            {
                if (readStream.ReadByte() == 0x0D)
                {
                    if (readStream.ReadByte() == 0x0A)
                    {
                        if (readStream.ReadUInt16() == 0x0A0D)
                            position = (int)readStream.BaseStream.Position;
                    }
                }
            }

            readStream.BaseStream.Position = 0;

            var data = readStream.ReadBytes(position - 4);
            var lines = Encoding.ASCII.GetString(data).Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            return Tuple.Create(lines, position);
        }

        public Tuple<string, string[], int> GetMessageType(byte[] data)
        {
            var headerParts = ReadLines(new BinaryReader(new MemoryStream(data)));

            if (headerParts.Item1.Length >= 2)
            {
                var identifier = headerParts.Item1[0].Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                if (identifier.Length == 3)
                    return Tuple.Create(identifier[1].Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)[0], headerParts.Item1, headerParts.Item2);
            }

            return Tuple.Create("Unknown", new string[0], 0);
        }

        public string GetClientInfo()
        {
            var ipEndPoint = client.RemoteEndPoint as IPEndPoint;

            return ipEndPoint != null ? ipEndPoint.Address + ":" + ipEndPoint.Port : "";
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    client.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
