// Copyright (c) Arctium.

using Framework.Misc;

using RealmServer.Constants.Net;
using RealmServer.Network;
using RealmServer.Network.Packets;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace RealmServer.Managers
{
    class ReplayManager : Singleton<ReplayManager>
    {
        public bool ReplayMode { get; set; }
        public bool CanSendCharacterList { get; set; }

        public List<SniffPacket> RawPackets;
        public int CurrentPacketId;
        public bool Paused;
        public bool Playing;
        public bool Logout;
        public uint CharId = 0; // UnitId
        public uint mountUnitId = 0; // UnitId
        public uint CurrentTime;
        public uint LastTime;

        // From the first client packet.
        public uint InitialTimevalue;
        public long InitialUnixTime;

        // Initial sniff time packet.
        public uint InitialSniffTime;

        public RealmSession session;

        Dictionary<int, Packet> movePackets;

        ReplayManager()
        {
            movePackets = new Dictionary<int, Packet>();
        }

        public void Assign(RealmSession rSession) => session = rSession;

        public bool Load(string path, bool skipmsg = false)
        {
            RawPackets = new List<SniffPacket>();

            if (!File.Exists(path))
            {
                Console.WriteLine($"ReplayManager::Load: Could not find file: '{path}'");
                return false;
            }

            foreach (var l in File.ReadAllLines(path))
            {
                var split = l.Split(';', StringSplitOptions.RemoveEmptyEntries);

                var pkt = new SniffPacket
                {
                    Timestamp = double.Parse(split[0].Substring(6)),
                    Direction = split[1].Contains("ClientMessage") ? true : false,
                    MessageId = int.Parse(split[2].Substring(13)),
                    Data = split[3].Substring(skipmsg ? 12 : 8).ToByteArray(),
                };

                if (pkt.Direction)
                {
                    if (pkt.MessageId != 0x7DD)
                    {
                        if (pkt.Data.Length >= 8 && BitConverter.ToUInt16(pkt.Data, 5) == 0x637)
                        {

                        }
                        else
                            continue;
                    }
                }
                else
                {
                    // Assign the initial UnitId here.
                    if (CharId == 0 && pkt.MessageId == 610)
                        CharId = BitConverter.ToUInt32(pkt.Data, 0);
                }

                RawPackets.Add(pkt);
            }

            GenerateMovementPackets();

            Console.WriteLine("SNIFF LOADED!");

            ReplayMode = true;

            return true;
        }

        class CommandEntry
        {
            public int Type { get; set; }
            public List<(int Bits, object Value)> Values = new List<(int Bits, object Value)>();
        }

        void GenerateMovementPackets()
        {
            for (var z = 0;z < RawPackets.Count; z++)
            {
                var p = RawPackets[z];

                if (p.Direction && p.Data.Length >= 8 && BitConverter.ToUInt16(p.Data, 5) == 0x637)
                {
                    var oldPkt = new Packet(p.Data.Skip(1).ToArray());
                    var time = oldPkt.Read<uint>(32);
                    var commandCount = oldPkt.Read<uint>(32);
                    var values = new List<CommandEntry>();
                    var platform = 0u;

                    if (InitialSniffTime == 0)
                    {
                        InitialSniffTime = time;
                    }

                    var currentUnixTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    // Get the packet time diff.
                    var timeDiff = currentUnixTime - InitialUnixTime;

                    // Set to new unix time.
                    InitialUnixTime = currentUnixTime;

                    if (InitialSniffTime == 0)
                        CurrentTime = time;
                    else
                        CurrentTime = (uint)timeDiff + InitialTimevalue;

                    LastTime = time;

                    for (var i = 0; i < commandCount; i++)
                    {
                        var commandType = oldPkt.Read<byte>(5);
                        var entry = new CommandEntry { Type = commandType };

                        switch (commandType)
                        {
                            case 1:
                                platform = oldPkt.Read<uint>(32);

                                //Console.ForegroundColor = ConsoleColor.Cyan;
                                //Console.WriteLine($"CURRENT PLATFORM ID: {platform}");
                                //Console.ForegroundColor = ConsoleColor.White;

                                entry.Values.Add((32, platform));
                                break;
                            case 0:
                                var curTime = oldPkt.Read<uint>(32);
                                var curTimeDiff = curTime - time;

                                entry.Values.Add((32, time));
                                break;
                            case 24:
                            case 27:
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                break;
                            case 2:
                            case 14:
                            case 19:
                                entry.Values.Add((32, oldPkt.Read<float>(32)));
                                entry.Values.Add((32, oldPkt.Read<float>(32)));
                                entry.Values.Add((32, oldPkt.Read<float>(32)));
                                entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                //Console.ForegroundColor = ConsoleColor.Yellow;
                                //Console.WriteLine("Got Position Change:");
                                //Console.WriteLine($"X: {entry.Values[0].Value}");
                                //Console.WriteLine($"Y: {entry.Values[1].Value}");
                                //Console.WriteLine($"Z: {entry.Values[2].Value}");
                                //Console.WriteLine();
                                //Console.ForegroundColor = ConsoleColor.White;
                                break;
                            case 3:
                            case 15:
                                var count = oldPkt.Read<uint>(10);

                                entry.Values.Add((10, count));

                                for (var j = 0; j < count; j++)
                                    entry.Values.Add((32, oldPkt.Read<uint>(32)));

                                for (var j = 0; j < count; j++)
                                {
                                    entry.Values.Add((32, oldPkt.Read<float>(32)));
                                    entry.Values.Add((32, oldPkt.Read<float>(32)));
                                    entry.Values.Add((32, oldPkt.Read<float>(32)));
                                }

                                entry.Values.Add((2, oldPkt.Read<uint>(2)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                break;
                            case 4:
                                count = oldPkt.Read<uint>(10);

                                entry.Values.Add((10, count));

                                for (var j = 0; j < count; j++)
                                {
                                    entry.Values.Add((32, oldPkt.Read<float>(32)));
                                    entry.Values.Add((32, oldPkt.Read<float>(32)));
                                    entry.Values.Add((32, oldPkt.Read<float>(32)));
                                }

                                entry.Values.Add((16, oldPkt.Read<ushort>(16)));
                                entry.Values.Add((2, oldPkt.Read<uint>(2)));
                                entry.Values.Add((4, oldPkt.Read<uint>(4)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                break;
                            case 5:
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));

                                entry.Values.Add((16, oldPkt.Read<float>(16)));
                                entry.Values.Add((16, oldPkt.Read<float>(16)));
                                entry.Values.Add((16, oldPkt.Read<float>(16)));

                                entry.Values.Add((4, oldPkt.Read<uint>(4)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                break;
                            case 6:
                                count = oldPkt.Read<uint>(10);

                                entry.Values.Add((10, count));

                                for (var j = 0; j < count; j++)
                                    entry.Values.Add((32, oldPkt.Read<uint>(32)));

                                entry.Values.Add((16, oldPkt.Read<ushort>(16)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((16, oldPkt.Read<float>(16)));
                                entry.Values.Add((16, oldPkt.Read<float>(16)));
                                entry.Values.Add((16, oldPkt.Read<float>(16)));
                                entry.Values.Add((4, oldPkt.Read<uint>(4)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                break;
                            case 7:
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                break;
                            case 8:
                            case 11:
                                entry.Values.Add((16, oldPkt.Read<float>(16)));
                                entry.Values.Add((16, oldPkt.Read<float>(16)));
                                entry.Values.Add((16, oldPkt.Read<float>(16)));
                                entry.Values.Add((1, oldPkt.Read<bool>(1)));

                                break;
                            case 9:
                            case 12:
                                count = oldPkt.Read<uint>(10);

                                entry.Values.Add((10, count));

                                for (var j = 0; j < count; j++)
                                    entry.Values.Add((32, oldPkt.Read<uint>(32)));

                                for (var j = 0; j < count; j++)
                                {
                                    entry.Values.Add((16, oldPkt.Read<float>(16)));
                                    entry.Values.Add((16, oldPkt.Read<float>(16)));
                                    entry.Values.Add((16, oldPkt.Read<float>(16)));
                                }

                                entry.Values.Add((2, oldPkt.Read<byte>(2)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                break;
                            case 10:
                            case 13:
                            case 21:
                            case 26:
                            case 29:
                                entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                break;
                            case 16:
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((16, oldPkt.Read<uint>(16)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((4, oldPkt.Read<uint>(4)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                break;
                            case 17:
                                count = oldPkt.Read<uint>(10);

                                entry.Values.Add((10, count));

                                for (var j = 0; j < count; j++)
                                    entry.Values.Add((32, oldPkt.Read<uint>(32)));

                                entry.Values.Add((16, oldPkt.Read<uint>(16)));

                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((4, oldPkt.Read<uint>(4)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                break;
                            case 18:
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                break;
                            case 20:
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((16, oldPkt.Read<uint>(16)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                break;
                            case 22:
                                entry.Values.Add((16, oldPkt.Read<uint>(16)));
                                entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                break;
                            case 23:
                                count = oldPkt.Read<uint>(8);

                                entry.Values.Add((8, count));

                                for (var j = 0; j < count; j++)
                                    entry.Values.Add((32, oldPkt.Read<uint>(32)));

                                for (var j = 0; j < count; j++)
                                    entry.Values.Add((16, oldPkt.Read<ushort>(16)));

                                entry.Values.Add((2, oldPkt.Read<uint>(2)));
                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                break;
                            case 25:
                            case 28:
                                count = oldPkt.Read<uint>(8);

                                entry.Values.Add((8, count));

                                for (var j = 0; j < count; j++)
                                    entry.Values.Add((32, oldPkt.Read<uint>(32)));

                                for (var j = 0; j < count; j++)
                                    entry.Values.Add((32, oldPkt.Read<uint>(32)));

                                entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                break;
                            default:
                                throw new NotImplementedException($"commandType {commandType} not implemented.");
                        }

                        values.Add(entry);
                    }

                    var stuff = values.ToArray();//.Where(v => v.Type == 0 || v.Type == 2 || v.Type == 8 || v.Type == 11 || v.Type == 24).ToArray();

                    var newPkt = new Packet((ServerMessage)0x638);

                    newPkt.Write(mountUnitId == 0 ? CharId : mountUnitId, 32);

                    //Console.WriteLine($"Sending time: {(LastTime != 0 ? (CurrentTime + (time - LastTime)) : LastTime)}");
                    var unixTime = (uint)DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    newPkt.Write(time, 32);
                    newPkt.Write(false, 1);
                    newPkt.Write(false, 1);

                    newPkt.Write(stuff.Length, 5);

                    LastTime = time;


                    for (var c = 0; c < stuff.Length; c++)
                    {
                        var command = stuff[c];

                        // 0, 2, 8, 11, 24

                        newPkt.Write(command.Type, 5);

                        /*if (command.Type == 0)
                        {
                            var cTime = (uint)command.Values[0].Value;
                            var timeDiff = time - cTime;

                            newPkt.Write(unixTime + timeDiff, 32);
                        }
                        else*/
                        {
                            command.Values.ForEach(v =>
                            {
                                if ((command.Type == 2 || command.Type == 8 || command.Type == 11) && v.Bits == 1)
                                    newPkt.Write(true, v.Bits);
                                else
                                    newPkt.Write(v.Value, v.Bits);
                            });
                        }
                    }

                    movePackets[z] = newPkt;
                    //session.Send(newPkt);
                }
            }
        }

        public void Play()
        {
            new Thread(() =>
            {
                PacketManager.ReplayEnabled = true;

                Playing = true;
                CurrentPacketId = 0;

                int waitMs = 0;
                SniffPacket p;

                while (CurrentPacketId < RawPackets.Count)
                {
                    p = RawPackets[CurrentPacketId];

                    // This is our first pause.
                    // Pre character list opcodes are sent until this one.
                    if (!CanSendCharacterList && p.MessageId == 0x117)
                    {
                        Paused = true;
                        CanSendCharacterList = true;
                    }

                    // We really want to pause here!
                    while (Paused)
                        Thread.Sleep(10);

                    // Skip and pause the client player login packet.
                    if (p.MessageId == 0x7DD)
                    {
                        CharId = 0;
                        CurrentPacketId++;
                        Paused = true;

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("[REPLAY] YOU CAN ENTER THE WORLD NOW!");
                        Console.ForegroundColor = ConsoleColor.White;

                        continue;
                    }

                    // Always use the latest UnitId
                    if (p.MessageId == 411)
                    {
                        CharId = BitConverter.ToUInt32(p.Data, 0);

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"CURRENT CHAR ID: {CharId}");
                        Console.ForegroundColor = ConsoleColor.White;

                    }

                    // Set the unit id for the mount
                    if (p.MessageId == 2159)
                    {
                        var pktLength = BitConverter.GetBytes(p.Data.Length + 6);
                        var mountPkt = new Packet(pktLength.Concat(BitConverter.GetBytes((ushort)p.MessageId)).Concat(p.Data).ToArray());

                        mountUnitId = mountPkt.Read<uint>(32);

                        mountPkt.Read<uint>(2);
                        mountPkt.Read<byte>(3);

                        if (mountPkt.Read<uint>(32) != CharId)
                        {
                            // That's not our char.
                            mountUnitId = 0;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"CURRENT MOUNT UNIT ID: {mountUnitId}");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                    }

                    // Remove the unit id for the mount
                    if (p.MessageId == 2247)
                    {
                        mountUnitId = BitConverter.ToUInt32(p.Data, 0);

                        if (BitConverter.ToUInt32(p.Data, 4) == CharId)
                        {
                            mountUnitId = 0;

                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"UNMOUNTED");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                    }



                    if (p.Direction && p.Data.Length >= 8 && BitConverter.ToUInt16(p.Data, 5) == 0x637)
                    {
                        session.Send(movePackets[CurrentPacketId]);

                        //CurrentPacketId++;
                        //continue;
                    }
                    else
                    {
                        var pkt = new Packet((ServerMessage)p.MessageId);

                        pkt.Write(p.Data);

                        session.Send(pkt);
                        /*
                        if (p.MessageId == 1592)
                        {
                            //if (BitConverter.ToUInt32(p.Data, 0) == 0x4007A)
                            {
                                var psize = BitConverter.GetBytes(p.Data.Length + 6);
                                var msgbytes = BitConverter.GetBytes((ushort)1592);
                                var oldPkt = new Packet(psize.Concat(msgbytes).Concat(p.Data).ToArray());
                                var unitId = oldPkt.Read<uint>(32);
                                var time = oldPkt.Read<uint>(32);
                                var resett = oldPkt.Read<bool>(1);
                                var serverc = oldPkt.Read<bool>(1);
                                var commandCount = oldPkt.Read<uint>(5);
                                var values = new List<CommandEntry>();
                                var platform = 0u;

                                for (var i = 0; i < commandCount; i++)
                                {
                                    var commandType = oldPkt.Read<byte>(5);
                                    var entry = new CommandEntry { Type = commandType };

                                    switch (commandType)
                                    {
                                        case 1:
                                            platform = oldPkt.Read<uint>(32);

                                            //Console.ForegroundColor = ConsoleColor.Cyan;
                                            //Console.WriteLine($"CURRENT PLATFORM ID: {platform}");
                                            //Console.ForegroundColor = ConsoleColor.White;

                                            entry.Values.Add((32, platform));
                                            break;
                                        case 0:
                                        case 24:
                                        case 27:
                                            entry.Values.Add((32, oldPkt.Read<ulong>(32)));
                                            break;
                                        case 2:
                                        case 14:
                                        case 19:
                                            entry.Values.Add((32, oldPkt.Read<float>(32)));
                                            entry.Values.Add((32, oldPkt.Read<float>(32)));
                                            entry.Values.Add((32, oldPkt.Read<float>(32)));
                                            entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                            //Console.ForegroundColor = ConsoleColor.Yellow;
                                            //Console.WriteLine("Got Position Change:");
                                            //Console.WriteLine($"X: {entry.Values[0].Value}");
                                            //Console.WriteLine($"Y: {entry.Values[1].Value}");
                                            //Console.WriteLine($"Z: {entry.Values[2].Value}");
                                            //Console.WriteLine();
                                            //Console.ForegroundColor = ConsoleColor.White;
                                            break;
                                        case 3:
                                        case 15:
                                            var count = oldPkt.Read<uint>(10);

                                            entry.Values.Add((10, count));

                                            for (var j = 0; j < count; j++)
                                                entry.Values.Add((32, oldPkt.Read<uint>(32)));

                                            for (var j = 0; j < count; j++)
                                            {
                                                entry.Values.Add((32, oldPkt.Read<float>(32)));
                                                entry.Values.Add((32, oldPkt.Read<float>(32)));
                                                entry.Values.Add((32, oldPkt.Read<float>(32)));
                                            }

                                            entry.Values.Add((2, oldPkt.Read<uint>(2)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                            break;
                                        case 4:
                                            count = oldPkt.Read<uint>(10);

                                            entry.Values.Add((10, count));

                                            for (var j = 0; j < count; j++)
                                            {
                                                entry.Values.Add((32, oldPkt.Read<float>(32)));
                                                entry.Values.Add((32, oldPkt.Read<float>(32)));
                                                entry.Values.Add((32, oldPkt.Read<float>(32)));
                                            }

                                            entry.Values.Add((16, oldPkt.Read<ushort>(16)));
                                            entry.Values.Add((2, oldPkt.Read<uint>(2)));
                                            entry.Values.Add((4, oldPkt.Read<uint>(4)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                            break;
                                        case 5:
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));

                                            entry.Values.Add((16, oldPkt.Read<float>(16)));
                                            entry.Values.Add((16, oldPkt.Read<float>(16)));
                                            entry.Values.Add((16, oldPkt.Read<float>(16)));

                                            entry.Values.Add((4, oldPkt.Read<uint>(4)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                            entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                            entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                            break;
                                        case 6:
                                            count = oldPkt.Read<uint>(10);

                                            entry.Values.Add((10, count));

                                            for (var j = 0; j < count; j++)
                                                entry.Values.Add((32, oldPkt.Read<uint>(32)));

                                            entry.Values.Add((16, oldPkt.Read<ushort>(16)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((16, oldPkt.Read<float>(16)));
                                            entry.Values.Add((16, oldPkt.Read<float>(16)));
                                            entry.Values.Add((16, oldPkt.Read<float>(16)));
                                            entry.Values.Add((4, oldPkt.Read<uint>(4)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                            entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                            break;
                                        case 7:
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                            break;
                                        case 8:
                                        case 11:
                                            entry.Values.Add((16, oldPkt.Read<float>(16)));
                                            entry.Values.Add((16, oldPkt.Read<float>(16)));
                                            entry.Values.Add((16, oldPkt.Read<float>(16)));
                                            entry.Values.Add((1, oldPkt.Read<bool>(1)));

                                            break;
                                        case 9:
                                        case 12:
                                            count = oldPkt.Read<uint>(10);

                                            entry.Values.Add((10, count));

                                            for (var j = 0; j < count; j++)
                                                entry.Values.Add((32, oldPkt.Read<uint>(32)));

                                            for (var j = 0; j < count; j++)
                                            {
                                                entry.Values.Add((16, oldPkt.Read<float>(16)));
                                                entry.Values.Add((16, oldPkt.Read<float>(16)));
                                                entry.Values.Add((16, oldPkt.Read<float>(16)));
                                            }

                                            entry.Values.Add((2, oldPkt.Read<byte>(2)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                            break;
                                        case 10:
                                        case 13:
                                        case 21:
                                        case 26:
                                        case 29:
                                            entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                            break;
                                        case 16:
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((16, oldPkt.Read<uint>(16)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((4, oldPkt.Read<uint>(4)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                            entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                            break;
                                        case 17:
                                            count = oldPkt.Read<uint>(10);

                                            entry.Values.Add((10, count));

                                            for (var j = 0; j < count; j++)
                                                entry.Values.Add((32, oldPkt.Read<uint>(32)));

                                            entry.Values.Add((16, oldPkt.Read<uint>(16)));

                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((4, oldPkt.Read<uint>(4)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                            break;
                                        case 18:
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                            break;
                                        case 20:
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((16, oldPkt.Read<uint>(16)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                            break;
                                        case 22:
                                            entry.Values.Add((16, oldPkt.Read<uint>(16)));
                                            entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                            break;
                                        case 23:
                                            count = oldPkt.Read<uint>(8);

                                            entry.Values.Add((8, count));

                                            for (var j = 0; j < count; j++)
                                                entry.Values.Add((32, oldPkt.Read<uint>(32)));

                                            for (var j = 0; j < count; j++)
                                                entry.Values.Add((16, oldPkt.Read<ushort>(16)));

                                            entry.Values.Add((2, oldPkt.Read<uint>(2)));
                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            entry.Values.Add((1, oldPkt.Read<bool>(1)));
                                            break;
                                        case 25:
                                        case 28:
                                            count = oldPkt.Read<uint>(8);

                                            entry.Values.Add((8, count));

                                            for (var j = 0; j < count; j++)
                                                entry.Values.Add((32, oldPkt.Read<uint>(32)));

                                            for (var j = 0; j < count; j++)
                                                entry.Values.Add((32, oldPkt.Read<uint>(32)));

                                            entry.Values.Add((32, oldPkt.Read<uint>(32)));
                                            break;
                                        default:
                                            throw new NotImplementedException($"commandType {commandType} not implemented.");
                                    }

                                    values.Add(entry);
                                }

                                var newPkt = new Packet((ServerMessage)0x638);

                                newPkt.Write(unitId, 32);

                                //Console.WriteLine($"Sending time: {(LastTime != 0 ? (CurrentTime + (time - LastTime)) : LastTime)}");

                                newPkt.Write(time, 32);
                                newPkt.Write(resett, 1);
                                newPkt.Write(serverc, 1);

                                newPkt.Write(commandCount, 5);
                                for (var c = 0; c < values.Count; c++)
                                {
                                    var command = values[c];

                                    // 0, 2, 8, 11, 24

                                    newPkt.Write(command.Type, 5);

                                    command.Values.ForEach(v =>
                                    {
                                        newPkt.Write(v.Value, v.Bits);
                                    });
                                }

                                session.Send(newPkt);
                                /* var curTime = BitConverter.ToUInt32(p.Data, 4);
                                 var pkt = new Packet((ServerMessage)p.MessageId);

                                 // 7A000400
                                 var charIdBytes = BitConverter.GetBytes(CharId);

                                 p.Data[0] = charIdBytes[0];
                                 p.Data[1] = charIdBytes[1];
                                 p.Data[2] = charIdBytes[2];
                                 p.Data[3] = charIdBytes[3];

                                 var timeBytes = BitConverter.GetBytes(CurrentTime);// LastTime != 0 ? (CurrentTime + (curTime - LastTime)) : LastTime);

                                 //Console.WriteLine($"Sending time: {(LastTime != 0 ? (CurrentTime + (curTime - LastTime)) : LastTime)}");

                                 LastTime = BitConverter.ToUInt32(p.Data, 4);

                                 p.Data[4] = timeBytes[0];
                                 p.Data[5] = timeBytes[1];
                                 p.Data[6] = timeBytes[2];
                                 p.Data[7] = timeBytes[3];

                                 pkt.Write(p.Data);

                        //session.Send(pkt);

                    }
                        }
                        else
                        {
                            var pkt = new Packet((ServerMessage)p.MessageId);

                            pkt.Write(p.Data);

                            session.Send(pkt);
                        }*/
                        //Console.WriteLine($"Sending {p.MessageId}");
                    }

                    // Pause after LOGGING OUT!
                    if (p.MessageId == 1428)
                        Logout = true;

                    // replay ended
                    if (CurrentPacketId == RawPackets.Count - 1)
                        break;

                    if (p.MessageId != 0x637 && RawPackets[CurrentPacketId + 1].Timestamp > p.Timestamp && RawPackets[CurrentPacketId + 1].MessageId != 0x7DD)
                        waitMs = (int)(RawPackets[CurrentPacketId + 1].Timestamp - p.Timestamp);
                    else
                        waitMs = 0;

                    if (p.MessageId == 97)
                        Thread.Sleep(4000);

                    if (waitMs > 0)
                        Thread.Sleep((int)waitMs);

                    CurrentPacketId++;
                }

                PacketManager.ReplayEnabled = true;
            })
            { IsBackground = true }.Start();
        }
    }
}
