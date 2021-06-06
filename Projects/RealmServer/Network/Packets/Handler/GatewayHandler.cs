// Copyright (c) Arctium.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Framework.Misc;
using RealmServer.Attributes;
using RealmServer.Constants.Net;
using RealmServer.Entities;
using RealmServer.Managers;
using static RealmServer.Managers.Manager;

namespace RealmServer.Network.Packets.Handler
{
    class GatewayHandler
    {
        [RealmPacket(ClientMessage.GatewayRequest)]
        public static void HandleGatewayRequest(Packet packet, RealmSession session)
        {
            var address = packet.Read<uint>(32);
            var gatewayTicket = packet.Read(16);
            var unknown2 = packet.Read<uint>(64);
            var loginName = packet.ReadWString();
            var unknown3 = packet.Read<uint>(32);

            session.Crypt = new Framework.Cryptography.PacketCrypt(gatewayTicket);

            // Only send these if the replay mode is disabled.
            if (ReplayManager.GetInstance().ReplayMode)
            {
                // This will send all pre character list opcodes.
                ReplayManager.GetInstance().Assign(session);
                ReplayManager.GetInstance().Play();
            }
        }


        //[RealmPacket(ClientMessage.LogoutRequest)]
        public static void HandleLogoutRequest(Packet packet, RealmSession session)
        {
            // Logout broadcast, not needed here
            var logout = new Packet(ServerMessage.Logout);

            logout.Write(1, 32);
            logout.Write(0, 1);

            // xxx
            logout.Write(0, 32);
            logout.Write(0, 32);

            for (var i = 120; i > 0; i -= 8)
                logout.Write(0, 64);

            for (var i = 152; i > 0; i -= 8)
                logout.Write(0, 64);

            session.Send(logout);

        }

        [RealmPacket(ClientMessage.LogoutRequest)]
        public static void HandleLogoutComplete(Packet packet, RealmSession session)
        {
            // Only send these if the replay mode is disabled.
            if (ReplayManager.GetInstance().ReplayMode)
            {
                // This will send all pre character list opcodes.
                //ReplayManager.GetInstance().Paused = true;
            }

            // direct logout
            var logout = new Packet(ServerMessage.LogoutComplete);

            logout.Write(1, 1); // requested
            logout.Write(0, 5); // reason

            session.Send(logout);

            DataMgr.UpdateCharacter(session.Character);
        }
        [RealmPacket(ClientMessage.RealmList)]
        public static void HandleRealmList(Packet packet, RealmSession session)
        {
            for (var i = 0; i < 0; i++)
            {
                var realmList = new Packet(ServerMessage.RealmList);

                realmList.Write(0, 64); // ServerTime
                realmList.Write(1, 32); // RealmCount

                // data
                realmList.Write(i, 32); // RealmId
                realmList.WriteWString("Arctium" + i); // RealmName
                realmList.Write(1337, 32); // NoteId!??!?!?!?
                realmList.Write(0, 32);

                realmList.Write(0, 32); // Type, 0 = PvE, 1 = PvP
                realmList.Write(4, 32); // Flags, 0 = Unknown, 1 = Offline, 2 = Down, 4 = Up
                realmList.Write(1, 32); // RealmStatus

                realmList.Write(0, 32);

                realmList.Write(0, 64);
                realmList.Write(0, 64);

                // selected char data
                realmList.Write(0, 14); // RealmId
                realmList.Write(0, 32); // CharacterCount
                realmList.WriteWString(""); // LastPlayedCharacter
                realmList.Write(0, 64); // LastPlayedTime

                realmList.Write(0, 16);
                realmList.Write(0, 16);
                realmList.Write(0, 16);
                realmList.Write(0, 16);

                realmList.Write(1, 32); // RealmMessagesCount
                realmList.Write(0, 32);

                {
                    realmList.Write(1, 8); // realmMessageLines
                    realmList.WriteWString("Yep it works!"); // RealmMessages
                }
                session.Send(realmList);

                Thread.Sleep(5000);
            }
        }

        [RealmPacket(ClientMessage.RetrieveCharacterList)]
        public static void HandleRetrieveCharacterList(Packet packet, RealmSession session)
        {
            Console.WriteLine("HandleRetrieveCharacterList");

            // Send all pre charcter list packets & the character list.
            if (ReplayManager.GetInstance().ReplayMode)
            {
                // Wait for the signal to send the character liust.
                while (!ReplayManager.GetInstance().CanSendCharacterList)
                    Thread.Sleep(1);

                // Continue with the character list packet.
                ReplayManager.GetInstance().Paused = false;
                return;
            }

            DataMgr = new Managers.DataManager();

            var pkt = new Packet(ServerMessage.AccountEntitlementUpdates);

            pkt.Write(3, 32);

            pkt.Write(12, 32); // Character Slot Unlock
            pkt.Write(12, 32); // 12 - disable purchase more buttons

            pkt.Write(61, 32); // Chua Warrior
            pkt.Write(2, 32);

            pkt.Write(62, 32); // Aurin Engineer
            pkt.Write(2, 32);

            session.Send(pkt);

            pkt = new Packet(ServerMessage.CharacterListResponse);

            pkt.Write(1, 64);
            pkt.Write(DataMgr.Characters.Count, 32); // charCount

            foreach (var kp in DataMgr.Characters)
            {
                var c = kp.Value;

                pkt.Write(c.Id, 64); // characterId
                pkt.WriteWString(c.Name); // characterName
                pkt.Write(c.Sex, 2); // characterSex
                pkt.Write(c.Race, 5); // characterRace
                pkt.Write(c.Class, 5); // characterClass
                pkt.Write(c.Faction, 32); // characterFaction
                pkt.Write(1, 32); // characterLevel = 1

                pkt.Write(c.Customizations.Count, 32); // bodyVisuals

                for (var j = 0; j < c.Customizations.Count; j++)
                {
                    var bv = c.Customizations[j];

                    pkt.Write(bv.ItemSlotId, 7); // itemSlot
                    pkt.Write(bv.ItemDisplayId, 15); // itemDisplayId
                    pkt.Write(0, 14); // itemColorSetId
                    pkt.Write(0, 32); // itemDyeData
                }

                pkt.Write(c.EquipmentVisuals.Count, 32); // equipmentVisuals

                for (var j = 0; j < c.EquipmentVisuals.Count; j++)
                {
                    var ev = c.EquipmentVisuals[j];

                    pkt.Write(ev[0], 7); // itemSlot
                    pkt.Write(ev[1], 15); // itemDisplayId
                    pkt.Write(ev[2], 14); // itemColorSetId
                    pkt.Write(ev[3], 32); // itemDyeData
                }

                pkt.Write(1, 15); // worldId
                pkt.Write(1, 15); // realmId
                pkt.Write(1, 14); // instanceId

                pkt.Write(0, 32); // X
                pkt.Write(0, 32); // Y
                pkt.Write(0, 32); // Z

                pkt.Write(0, 32); // Yaw
                pkt.Write(0, 32); // Pitch

                pkt.Write(c.Path, 3); // characterPath
                pkt.Write(0, 1); // characterLocked
                pkt.Write(0, 1); // 
                pkt.Write(4294967295, 32);

                // First uint[]
                pkt.Write(0, 4);

                // Second uint[], Size of first uint[]

                // Third uint[]
                var thirdArray = new uint[0];

                pkt.Write(thirdArray.Length, 32);

                foreach (var e in thirdArray)
                    pkt.Write(e, 32);

                pkt.Write(3240842315, 32);
            }

            // Fourth uint[]
            var fourthArray = new uint[0];

            pkt.Write(fourthArray.Length, 32);
            foreach (var e in fourthArray)
                pkt.Write(e, 32);

            // Fifth uint[]
            var fifthArray = new uint[0];

            pkt.Write(fifthArray.Length, 32);
            foreach (var e in fifthArray)
                pkt.Write(e, 32);

            pkt.Write(0, 14);
            pkt.Write(0, 14);
            pkt.Write(0, 64);
            pkt.Write(0, 32);
            pkt.Write(0, 32);
            pkt.Write(12, 32); 
            pkt.Write(1, 32);
            pkt.Write(0, 14);
            pkt.Write(0, 1);

            session.Send(pkt);

        }

        [RealmPacket(ClientMessage.CreateCharacter)]
        public static void HandleCreateCharacter(Packet packet, RealmSession session)
        {
            Console.WriteLine("HandleCreateCharacter");
            var character = new Character();

            var characterCreationId = packet.Read<uint>();
            var characterCreation = TableMgr.CharacterCreations.SingleOrDefault(cc => cc.Id == characterCreationId);
            var name = packet.ReadWString();
            var path = packet.Read<byte>(3);
            var bodyVisualIds = packet.ReadUIntArray();
            var bodyVisualValues = packet.ReadUIntArray(bodyVisualIds.Length);
            var characterCustomizations = packet.ReadUIntArray();

            var bodyViuals = new Dictionary<uint, uint>();

            for (var i = 0; i < bodyVisualIds.Length; i++)
                bodyViuals.Add(bodyVisualIds[i], bodyVisualValues[i]);

            character.Id = (uint)new Random(Environment.TickCount).Next();
            character.Name = name;
            character.Path = path;
            character.Class = characterCreation.ClassId;
            character.Race = characterCreation.RaceId;
            character.Sex = characterCreation.Sex;
            character.Faction = characterCreation.FactionId;
            character.Customizations = new List<CharacterCustomization>(bodyVisualIds.Length);

            // Process body visuals (basic customizations)
            for (var i = 0; i < bodyVisualIds.Length; i++)
            {
                var labelId = TableMgr.CharacterCustomizationLabels.SingleOrDefault(ccl => ccl.Id == bodyVisualIds[i]);
                var customization = TableMgr.CharacterCustomizations.Where(cc =>
                                    cc.RaceId == character.Race && cc.Gender == character.Sex &&
                                    cc.CharacterCustomizationLabelId00 == labelId.Id && cc.Value00 == bodyVisualValues[i]).ToArray();

                if (customization.Length == 1)
                {
                    // Easy stuff... just add it.
                    character.Customizations.Add(customization[0]);
                }
                else if (customization.Length > 1)
                {
                    // Get the label id to create our unique result.
                    var labelId01 = customization.First(c => c.CharacterCustomizationLabelId01 != 0).CharacterCustomizationLabelId01;

                    // Sometimes this can be null.
                    var realCustomization = customization.SingleOrDefault(c => c.Value01 == bodyViuals[labelId01]);

                    // In this case there should be exactly ONE entry with CharacterCustomizationLabelId01 = 0 that we use.
                    // Otherwise an exception is intended here.
                    if (realCustomization == null)
                        realCustomization = customization.SingleOrDefault(c => c.CharacterCustomizationLabelId01 == 0);

                    if (realCustomization == null)
                        Console.WriteLine($"Warning: Can't find customization for {bodyVisualIds[i]}/{bodyVisualValues[i]}");
                    else
                        character.Customizations.Add(realCustomization);
                }
            }

            character.BoneCustomizations = characterCustomizations;

            var charCreateArmorSet = TableMgr.CharacterCreationArmorSet.SingleOrDefault(ccas => ccas.ClassId == character.Class && ccas.CreationGearSetEnum == 0);

            // equipVisuals
            character.EquipmentVisuals = new List<uint[]>();

            void FillEquip(uint itemDisplayId)
            {
                if (itemDisplayId != 0)
                {
                    var itemDisplay = TableMgr.ItemDisplay.Single(id => id.Id == itemDisplayId);

                    // { 1, 3233, 0, 0 }
                    character.EquipmentVisuals.Add(new uint[] { TableMgr.Item2Type.Single(i2t => i2t.Id == itemDisplay.Item2TypeId).ItemSlotId, itemDisplayId, 0, 0 });
                }
            }

            // Let's use all 16
            FillEquip(charCreateArmorSet.ItemDisplayId00);
            FillEquip(charCreateArmorSet.ItemDisplayId01);
            FillEquip(charCreateArmorSet.ItemDisplayId02);
            FillEquip(charCreateArmorSet.ItemDisplayId03);
            FillEquip(charCreateArmorSet.ItemDisplayId04);
            FillEquip(charCreateArmorSet.ItemDisplayId05);
            FillEquip(charCreateArmorSet.ItemDisplayId06);
            FillEquip(charCreateArmorSet.ItemDisplayId07);
            FillEquip(charCreateArmorSet.ItemDisplayId08);
            FillEquip(charCreateArmorSet.ItemDisplayId09);
            FillEquip(charCreateArmorSet.ItemDisplayId10);
            FillEquip(charCreateArmorSet.ItemDisplayId11);

            // Set start location
            character.Location = new System.Numerics.Vector3
            {
                X = -771.823f,
                Y = -904.28523f,
                Z = -2269.56f
            };

            character.WorldId = 990;

            DataMgr.Add(character);


            var pkt = new Packet(ServerMessage.CharacterCreateResult);

            pkt.Write(character.Id, 64);
            pkt.Write(1, 32);
            pkt.Write(3, 6); // 3 = Success

            session.Send(pkt);

        }

        [RealmPacket(ClientMessage.DeleteCharacter)]
        public static void HandleDeleteCharacter(Packet packet, RealmSession session)
        {
            var characterId = packet.Read<ulong>(64);

            DataMgr.RemoveCharacterById(characterId);

            var characterDeleteResult = new Packet(ServerMessage.CharacterDeleteResult);

            // 0 - DeleteOk
            if (!DataMgr.Characters.Any(c => c.Key == characterId))
                characterDeleteResult.Write(0, 6);

            characterDeleteResult.Write(0, 32);

            session.Send(characterDeleteResult);
        }

        [RealmPacket(ClientMessage.PlayerLogin)]
        public static void HandlePlayerLogin(Packet packet, RealmSession session)
        {
            var characterId = packet.Read<ulong>(64);

            Console.WriteLine($"CharacterID (PlayerLogin): {characterId}");

            if (ReplayManager.GetInstance().ReplayMode)
            {
                //ReplayManager.GetInstance().CharId = (uint)characterId;

                // Just continue the current replay here.
                // There are not more steps required for now.
                ReplayManager.GetInstance().Paused = false;
                ReplayManager.GetInstance().CanSendCharacterList = false;

                return;
            }

            session.Character = DataMgr.Characters.Single(c => c.Key == characterId).Value;

            LoginToWorld(session.Character, session);
        }

        public static void LoginToWorld(Character character, RealmSession session)
        {
            // triggers the loading screen.
            var worldLogin = new Packet((ServerMessage)173);
            // WorldId (15 bits), X, Y, Z, Yaw, Pitch
            worldLogin.Write(character.WorldId, 15);
            worldLogin.WriteFloat(character.Location.X, 32);
            worldLogin.WriteFloat(character.Location.Y, 32);
            worldLogin.WriteFloat(character.Location.Z, 32);
            worldLogin.WriteFloat(0, 32);
            worldLogin.WriteFloat(0, 32);

            session.Send(worldLogin);

            CreatePlayer(session);

            // client freeze without it
            worldLogin = new Packet((ServerMessage)411);
            worldLogin.Write("A151040001000000".ToByteArray());
            session.Send(worldLogin);

            // prevents path tracking lua error
            worldLogin = new Packet((ServerMessage)1724);
            worldLogin.Write("0100000000000000000000000000000010127C0A64".ToByteArray());
            session.Send(worldLogin);

            // required...
            worldLogin = new Packet((ServerMessage)241);
            worldLogin.Write("0000000000F401000000".ToByteArray());
            session.Send(worldLogin);

            // enables movement (keybindings?!)
            worldLogin = new Packet((ServerMessage)1590);
            worldLogin.Write("0100000043A3080000".ToByteArray());

            session.Send(worldLogin);

            var pkt = new Packet(ServerMessage.CharacterCreated);

            pkt.Write("07000000C9AC2420000000150000000000000000D23D010000000008000000000000000000000000000008000000000000000000000000000000FC01000000680000000000000000000000000000000000000000000000000000000000000000000000000031322B09080000400500000000000000C0744F0002000000020000000000000000000000000000020000000000000000000000000000007F000000001A00000000000000000000000000000000000000000000000000000000000000000000000040ECCC4A0202000050010000000000000000DE138001000080000000000000000000000000000080000000000000000000000000000000C01F0000008006000000000000000000000000000000000000000000000000000000000000000000000000102BB3928000000054000000000000000050F7048000000020000000000000000000000000000020000000000000000000000000000000F007000000A001000000000000000000000000000000000000000000000000000000000000000000000000C4CDAC2420000000150000000000000000DF3D012800000008000000000000000000000000000008000000000000000000000000000000FC010000006800000000000000000000000000000000000000000000000000000000000000000000000000F1322B09080000400500000000000000805508001E000000020000000000000000000000000000000000000000000000000000000000007F000000001A00000000000000000000000000000000000000000000000000000000000000000000000040CCCC4A0202000050010000000000000070DC130008000080000000000000000000000000000080000000000000000000000000000000C01F00000080060000000000000000000000000000000000000000000000000000000000000000000000001003000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000200010000000000000000000000000000000E014000000000000000000000000F8FFFFFF0700000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000034332781000000000000000000".ToByteArray()); session.Send(pkt);

            // finish world login
            worldLogin = new Packet((ServerMessage)97);
            session.Send(worldLogin);

            // Test world zone spawns.
            if (character.WorldId == 990)
                TestHandler.TestSpawnHandler(session);

        }

        public static void CreatePlayer(RealmSession session)
        {
            var character = session.Character;
            // create object.
            var worldLogin = new Packet((ServerMessage)610);

            worldLogin.Write(283041, 32); // UnitId
            worldLogin.Write(20, 6); // UnitType, Player

            // if type == Player
            if (true)
            {
                worldLogin.Write(character.Id, 64);
                worldLogin.Write(1, 14); // InstanceId

                worldLogin.WriteWString(character.Name);

                worldLogin.Write(character.Race, 5);
                worldLogin.Write(character.Class, 5);
                worldLogin.Write(character.Sex, 2);

                worldLogin.Write(0, 64); // GroupId?

                worldLogin.Write(0, 8); //PetCount

                worldLogin.Write(false, 1); // guildname
                worldLogin.Write(0, 7);
                worldLogin.Write(0, 4); // GuildType?
                worldLogin.Write(0, 5); // GuildIdCount?

                worldLogin.Write(character.BoneCustomizations.Length, 6); // BoneCustomizations

                foreach (var bc in character.BoneCustomizations)
                    worldLogin.Write(bc, 32);

                worldLogin.Write(0, 3);
                worldLogin.Write(0, 8);
                worldLogin.Write(0, 14);
            }

            worldLogin.Write(0x1, 8); // CreateFlags

            // Vitals/Stat
            worldLogin.Write(19, 5); // count

            worldLogin.Write(0, 5); // Health
            worldLogin.Write(0, 2);
            worldLogin.Write(1200, 32);

            worldLogin.Write(1, 5);
            worldLogin.Write(1, 2);
            worldLogin.Write(0, 32);

            worldLogin.Write(2, 5);
            worldLogin.Write(1, 2);
            worldLogin.Write(1140457472, 32);

            worldLogin.Write(3, 5);
            worldLogin.Write(1, 2);
            worldLogin.Write(0, 32);

            worldLogin.Write(4, 5);
            worldLogin.Write(1, 2);
            worldLogin.Write(0, 32);

            worldLogin.Write(5, 5);
            worldLogin.Write(1, 2);
            worldLogin.Write(0, 32);

            worldLogin.Write(6, 5);
            worldLogin.Write(1, 2);
            worldLogin.Write(0, 32);

            worldLogin.Write(7, 5);
            worldLogin.Write(1, 2);
            worldLogin.Write(0, 32);

            worldLogin.Write(8, 5);
            worldLogin.Write(1, 2);
            worldLogin.Write(0, 32);

            worldLogin.Write(9, 5);
            worldLogin.Write(1, 2);
            worldLogin.Write(1128792064, 32);

            worldLogin.Write(23, 5);
            worldLogin.Write(1, 2);
            worldLogin.Write(0, 32);

            worldLogin.Write(24, 5);
            worldLogin.Write(1, 2);
            worldLogin.Write(0, 32);

            worldLogin.Write(25, 5);
            worldLogin.Write(1, 2);
            worldLogin.Write(0, 32);

            worldLogin.Write(10, 5); // Level
            worldLogin.Write(0, 2);
            worldLogin.Write(1, 32);

            worldLogin.Write(15, 5);
            worldLogin.Write(0, 2);
            worldLogin.Write(1, 32);

            worldLogin.Write(19, 5);
            worldLogin.Write(1, 2);
            worldLogin.Write(1120403456, 32);

            worldLogin.Write(20, 5); // shield
            worldLogin.Write(0, 2);
            worldLogin.Write(450, 32);

            worldLogin.Write(21, 5);
            worldLogin.Write(0, 2);
            worldLogin.Write(0, 32);

            worldLogin.Write(22, 5);
            worldLogin.Write(0, 2);
            worldLogin.Write(0, 32);


            worldLogin.Write((uint)DateTimeOffset.Now.ToUnixTimeMilliseconds(), 32); // Time?

            /// Commands
            worldLogin.Write(8, 5); // CommandCount
            worldLogin.Write(0, 5);
            worldLogin.Write(0, 32); // time
            // SetPlatform
            worldLogin.Write(1, 5);
            worldLogin.Write(0, 32); // UnitId
            // SetPosition
            worldLogin.Write(2, 5);
            worldLogin.WriteFloat(character.Location.X, 32);
            worldLogin.WriteFloat(character.Location.Y, 32);
            worldLogin.WriteFloat(character.Location.Z, 32);
            worldLogin.Write(false, 1);           // Blend
            // SetVelocity
            worldLogin.Write(8, 5);
            worldLogin.Write(0, 16); // X
            worldLogin.Write(0, 16); // Y
            worldLogin.Write(0, 16); // Z
            worldLogin.Write(false, 1);  // Blend
            // SetMove
            worldLogin.Write(11, 5);
            worldLogin.Write(0, 16); // X
            worldLogin.Write(0, 16); // Y
            worldLogin.Write(0, 16); // Z
            worldLogin.Write(false, 1);  // Blend
            // SetRotation
            worldLogin.Write(14, 5);
            worldLogin.Write(0, 32); // Yaw
            worldLogin.Write(0, 32);          // Pitch
            worldLogin.Write(0, 32);          // Roll
            worldLogin.Write(false, 1);           // Blend
            // SetStateDefault
            worldLogin.Write(26, 5);
            worldLogin.Write(false, 1); // Strafe
            // SetModeDefault
            worldLogin.Write(29, 5);
            worldLogin.Write(false, 1); // Unused

            var propertyCount = 34;

            if (character.MoveSpeed != 0)
                propertyCount += 1;

            if (character.JumpHeight != 0)
                propertyCount += 1;

            // properties
            worldLogin.Write(propertyCount, 8); // count

            worldLogin.Write(0, 8); // Strength
            worldLogin.Write(0, 32);
            worldLogin.Write(0, 32);

            worldLogin.Write(1, 8); // Dexterity
            worldLogin.Write(0, 32);
            worldLogin.Write(0, 32);

            worldLogin.Write(2, 8); // Technology
            worldLogin.Write(0, 32);
            worldLogin.Write(0, 32);

            worldLogin.Write(3, 8); // Magic
            worldLogin.Write(0, 32);
            worldLogin.Write(0, 32);

            worldLogin.Write(4, 8); // Wisdom
            worldLogin.Write(0, 32);
            worldLogin.Write(0, 32);

            worldLogin.Write(5, 8); // BaseFocusPool
            worldLogin.Write(1148846080, 32);
            worldLogin.Write(1148846080, 32);

            worldLogin.Write(7, 8); // BaseHealth
            worldLogin.Write(1142292480, 32);
            worldLogin.Write(1150681088, 32);

            worldLogin.Write(8, 8); // HealthRegenMultiplier
            worldLogin.Write(1024470496, 32);
            worldLogin.Write(1024470496, 32);

            worldLogin.Write(9, 8); // ResourceMax_0
            worldLogin.Write(1140457472, 32);
            worldLogin.Write(1140457472, 32);

            worldLogin.Write(10, 8); // ResourceMax_1
            worldLogin.Write(1084227584, 32);
            worldLogin.Write(1084227584, 32);

            worldLogin.Write(16, 8); // ResourceRegenMultiplier_0
            worldLogin.Write(1018712556, 32);
            worldLogin.Write(1018712556, 32);

            worldLogin.Write(35, 8); // AssaultRating
            worldLogin.Write(1113063424, 32);
            worldLogin.Write(1132496896, 32);

            worldLogin.Write(36, 8); // SupportRating
            worldLogin.Write(1113063424, 32);
            worldLogin.Write(1121673216, 32);

            worldLogin.Write(38, 8); // ResourceMax_7
            worldLogin.Write(1128792064, 32);
            worldLogin.Write(1128792064, 32);

            worldLogin.Write(39, 8); // ResourceRegenMultiplier_7
            worldLogin.Write(1027101164, 32);
            worldLogin.Write(1027101164, 32);

            worldLogin.Write(40, 8); // Stamina
            worldLogin.Write(0, 32);
            worldLogin.Write(0, 32);

            worldLogin.Write(41, 8); // ShieldCapacityMax
            worldLogin.Write(0, 32);
            worldLogin.Write(1138819072, 32);

            worldLogin.Write(42, 8); // Armor
            worldLogin.Write(0, 32);
            worldLogin.Write(1125515264, 32);

            if (character.MoveSpeed != 0)
            {
                // Client uses default without these data
                worldLogin.Write(100, 8); // MoveSpeedMultiplier
                worldLogin.WriteFloat(character.MoveSpeed, 32);
                worldLogin.WriteFloat(character.MoveSpeed, 32);
            }

            worldLogin.Write(101, 8); // BaseAvoidChance
            worldLogin.Write(1028443341, 32);
            worldLogin.Write(1028443341, 32);

            worldLogin.Write(102, 8); // BaseCritChance
            worldLogin.Write(1028443341, 32);
            worldLogin.Write(1028443341, 32);

            worldLogin.Write(107, 8); // BaseFocusRecoveryInCombat
            worldLogin.Write(1000593162, 32);
            worldLogin.Write(1000593162, 32);

            worldLogin.Write(108, 8); // BaseFocusRecoveryOutofCombat
            worldLogin.Write(1017370378, 32);
            worldLogin.Write(1017370378, 32);

            worldLogin.Write(112, 8); // BaseMultiHitAmount
            worldLogin.Write(1050253722, 32);
            worldLogin.Write(1050253722, 32);

            if (character.JumpHeight != 0)
            {
                // Client uses default without these data
                worldLogin.Write(129, 8); // JumpHeight
                worldLogin.WriteFloat(character.JumpHeight, 32);
                worldLogin.WriteFloat(character.JumpHeight, 32);
            }

            if (character.GravityMultiplier != 0)
            {
                worldLogin.Write(130, 8); // GravityMultiplier
                worldLogin.WriteFloat(character.GravityMultiplier, 32);
                worldLogin.WriteFloat(character.GravityMultiplier, 32);
            }
            else
            {
                worldLogin.Write(130, 8); // GravityMultiplier
                worldLogin.WriteFloat(0.8f, 32);
                worldLogin.WriteFloat(0.8f, 32);
            }

            worldLogin.Write(150, 8); // DamageTakenOffsetPhysical
            worldLogin.Write(1065353216, 32);
            worldLogin.Write(1065353216, 32);

            worldLogin.Write(151, 8); // DamageTakenOffsetTech
            worldLogin.Write(1065353216, 32);
            worldLogin.Write(1065353216, 32);

            worldLogin.Write(152, 8); // DamageTakenOffsetMagic
            worldLogin.Write(1065353216, 32);
            worldLogin.Write(1065353216, 32);

            worldLogin.Write(154, 8); // BaseMultiHitChance
            worldLogin.Write(1028443341, 32);
            worldLogin.Write(1028443341, 32);

            worldLogin.Write(155, 8); // BaseDamageReflectAmount
            worldLogin.Write(1028443341, 32);
            worldLogin.Write(1028443341, 32);

            worldLogin.Write(175, 8); // ShieldMitigationMax
            worldLogin.Write(0, 32);
            worldLogin.Write(1059061760, 32);

            worldLogin.Write(176, 8); // ShieldRegenPct
            worldLogin.Write(0, 32);
            worldLogin.Write(1041865114, 32);

            worldLogin.Write(177, 8); // ShieldTickTime
            worldLogin.Write(1132068864, 32);
            worldLogin.Write(1132068864, 32);

            worldLogin.Write(178, 8); // ShieldRebootTime
            worldLogin.Write(1159479296, 32);
            worldLogin.Write(1168123904, 32);

            worldLogin.Write(195, 8); // BaseGlanceAmount
            worldLogin.Write(1050253722, 32);
            worldLogin.Write(1050253722, 32);

            if (session.Character.DisplayInfoId == 0)
            {
                //bodyVisuals/equip
                worldLogin.Write(character.Customizations.Count + character.EquipmentVisuals.Count, 7); // bodyVisuals

                for (var j = 0; j < character.Customizations.Count; j++)
                {
                    var bv = character.Customizations[j];

                    worldLogin.Write(bv.ItemSlotId, 7); // itemSlot
                    worldLogin.Write(bv.ItemDisplayId, 15); // itemDisplayId
                    worldLogin.Write(0, 14); // itemColorSetId
                    worldLogin.Write(0, 32); // itemDyeData
                }

                for (var j = 0; j < character.EquipmentVisuals.Count; j++)
                {
                    var bv = character.EquipmentVisuals[j];

                    worldLogin.Write(bv[0], 7); // itemSlot
                    worldLogin.Write(bv[1], 15); // itemDisplayId
                    worldLogin.Write(bv[2], 14); // itemColorSetId
                    worldLogin.Write(bv[3], 32); // itemDyeData
                }
            }
            else
                worldLogin.Write(0, 7);

            // spell stuff
            worldLogin.Write(0, 9);

            worldLogin.Write(0, 32);
            worldLogin.Write(character.Faction, 14);
            worldLogin.Write(character.Faction, 14);
            worldLogin.Write(0, 32);
            worldLogin.Write(0, 64);
            worldLogin.Write(0, 2);
            worldLogin.Write(false, 1);
            worldLogin.Write(0, 2);
            worldLogin.Write(false, 1);
            worldLogin.Write(0, 2);
            worldLogin.Write(false, 1);
            worldLogin.Write(0, 14);
            worldLogin.Write(character.DisplayInfoId, 17);
            worldLogin.Write(0, 15);

            session.Send(worldLogin);
        }

        class CommandEntry
        {
            public int Type { get; set; }
            public List<(int Bits, object Value)> Values = new List<(int Bits, object Value)>();
        }

        [RealmPacket(ClientMessage.UpdateCommand)]
        public static void HandleUpdateCommand(Packet packet, RealmSession session)
        {
            var pkt = packet;
            var time = pkt.Read<uint>(32);
            var commandCount = pkt.Read<uint>(32);
            var values = new List<CommandEntry>();
            var platform = 0u;

            if (ReplayManager.GetInstance().InitialTimevalue == 0)
            {
                ReplayManager.GetInstance().InitialTimevalue = time;
                ReplayManager.GetInstance().InitialUnixTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }

            for (var i = 0; i < commandCount; i++)
            {
                Console.WriteLine();
                var commandType = pkt.Read<byte>(5, "commandType");
                var entry = new CommandEntry { Type = commandType };

                switch (commandType)
                {
                    case 0:
                        var tiemCmd = pkt.Read<uint>(32, "Time");

                        entry.Values.Add((32,tiemCmd ));
                        break;
                    case 1:
                        platform = pkt.Read<uint>(32);

                        entry.Values.Add((32, platform));
                        break;
                    case 24:
                        entry.Values.Add((32, pkt.Read<uint>(32, "State ")));
                        break;
                    case 27:
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        break;
                    case 2:
                        entry.Values.Add((32, pkt.Read<float>(32, "X")));
                        entry.Values.Add((32, pkt.Read<float>(32, "Y")));
                        entry.Values.Add((32, pkt.Read<float>(32, "Z")));
                        entry.Values.Add((1, pkt.Read<bool>(1, "Blend")));
                        break;
                    case 14:
                        entry.Values.Add((32, pkt.Read<float>(32, "X")));
                        entry.Values.Add((32, pkt.Read<float>(32, "Y")));
                        entry.Values.Add((32, pkt.Read<float>(32, "Z")));
                        entry.Values.Add((1, pkt.Read<bool>(1, "Blend")));
                        break;
                    case 19:
                        entry.Values.Add((32, pkt.Read<float>(32, "X")));
                        entry.Values.Add((32, pkt.Read<float>(32, "Y")));
                        entry.Values.Add((32, pkt.Read<float>(32, "Z")));
                        entry.Values.Add((1, pkt.Read<bool>(1, "Blend")));
                        break;
                    case 3:
                        var count = pkt.Read<uint>(10);

                        entry.Values.Add((10, count));

                        for (var j = 0; j < count; j++)
                            entry.Values.Add((32, pkt.Read<uint>(32)));

                        for (var j = 0; j < count; j++)
                        {
                            entry.Values.Add((32, pkt.Read<float>(32)));
                            entry.Values.Add((32, pkt.Read<float>(32)));
                            entry.Values.Add((32, pkt.Read<float>(32)));
                        }

                        entry.Values.Add((2, pkt.Read<uint>(2)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        break;
                    case 15:
                        count = pkt.Read<uint>(10);

                        entry.Values.Add((10, count));

                        for (var j = 0; j < count; j++)
                            entry.Values.Add((32, pkt.Read<uint>(32)));

                        for (var j = 0; j < count; j++)
                        {
                            entry.Values.Add((32, pkt.Read<float>(32)));
                            entry.Values.Add((32, pkt.Read<float>(32)));
                            entry.Values.Add((32, pkt.Read<float>(32)));
                        }

                        entry.Values.Add((2, pkt.Read<uint>(2)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        break;
                    case 4:
                        count = pkt.Read<uint>(10);

                        entry.Values.Add((10, count));

                        for (var j = 0; j < count; j++)
                        {
                            entry.Values.Add((32, pkt.Read<float>(32)));
                            entry.Values.Add((32, pkt.Read<float>(32)));
                            entry.Values.Add((32, pkt.Read<float>(32)));
                        }

                        entry.Values.Add((16, pkt.Read<ushort>(16)));
                        entry.Values.Add((2, pkt.Read<uint>(2)));
                        entry.Values.Add((4, pkt.Read<uint>(4)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        break;
                    case 5:
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));

                        entry.Values.Add((16, pkt.Read<float>(16)));
                        entry.Values.Add((16, pkt.Read<float>(16)));
                        entry.Values.Add((16, pkt.Read<float>(16)));

                        entry.Values.Add((4, pkt.Read<uint>(4)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        break;
                    case 6:
                        count = pkt.Read<uint>(10);

                        entry.Values.Add((10, count));

                        for (var j = 0; j < count; j++)
                            entry.Values.Add((32, pkt.Read<uint>(32)));

                        entry.Values.Add((16, pkt.Read<ushort>(16)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((16, pkt.Read<float>(16)));
                        entry.Values.Add((16, pkt.Read<float>(16)));
                        entry.Values.Add((16, pkt.Read<float>(16)));
                        entry.Values.Add((4, pkt.Read<uint>(4)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        break;
                    case 7:
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        break;
                    case 8:
                        entry.Values.Add((16, pkt.Read<float>(16, "X")));
                        entry.Values.Add((16, pkt.Read<float>(16, "Y")));
                        entry.Values.Add((16, pkt.Read<float>(16, "Z")));
                        entry.Values.Add((1, pkt.Read<bool>(1, "Blend")));

                        break;
                    case 11:
                        entry.Values.Add((16, pkt.Read<float>(16, "X")));
                        entry.Values.Add((16, pkt.Read<float>(16, "Y")));
                        entry.Values.Add((16, pkt.Read<float>(16, "Z")));
                        entry.Values.Add((1, pkt.Read<bool>(1, "Blend")));

                        break;
                    case 9:
                        count = pkt.Read<uint>(10);

                        entry.Values.Add((10, count));

                        for (var j = 0; j < count; j++)
                            entry.Values.Add((32, pkt.Read<uint>(32)));

                        for (var j = 0; j < count; j++)
                        {
                            entry.Values.Add((16, pkt.Read<float>(16)));
                            entry.Values.Add((16, pkt.Read<float>(16)));
                            entry.Values.Add((16, pkt.Read<float>(16)));
                        }

                        entry.Values.Add((2, pkt.Read<byte>(2)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        break;
                    case 12:
                        count = pkt.Read<uint>(10);

                        entry.Values.Add((10, count));

                        for (var j = 0; j < count; j++)
                            entry.Values.Add((32, pkt.Read<uint>(32)));

                        for (var j = 0; j < count; j++)
                        {
                            entry.Values.Add((16, pkt.Read<float>(16)));
                            entry.Values.Add((16, pkt.Read<float>(16)));
                            entry.Values.Add((16, pkt.Read<float>(16)));
                        }

                        entry.Values.Add((2, pkt.Read<byte>(2)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        break;
                    case 10:
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        break;
                    case 13:
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        break;
                    case 21:
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        break;
                    case 26:
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        break;
                    case 29:
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        break;
                    case 16:
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((16, pkt.Read<uint>(16)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((4, pkt.Read<uint>(4)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        break;
                    case 17:
                        count = pkt.Read<uint>(10);

                        entry.Values.Add((10, count));

                        for (var j = 0; j < count; j++)
                            entry.Values.Add((32, pkt.Read<uint>(32)));

                        entry.Values.Add((16, pkt.Read<uint>(16)));

                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((4, pkt.Read<uint>(4)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        break;
                    case 18:
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        break;
                    case 20:
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((16, pkt.Read<uint>(16)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        break;
                    case 22:
                        entry.Values.Add((16, pkt.Read<uint>(16)));
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        break;
                    case 23:
                        count = pkt.Read<uint>(8);

                        entry.Values.Add((8, count));

                        for (var j = 0; j < count; j++)
                            entry.Values.Add((32, pkt.Read<uint>(32)));

                        for (var j = 0; j < count; j++)
                            entry.Values.Add((16, pkt.Read<ushort>(16)));

                        entry.Values.Add((2, pkt.Read<uint>(2)));
                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        entry.Values.Add((1, pkt.Read<bool>(1)));
                        break;
                    case 25:
                        count = pkt.Read<uint>(8);

                        entry.Values.Add((8, count));

                        for (var j = 0; j < count; j++)
                            entry.Values.Add((32, pkt.Read<uint>(32)));

                        for (var j = 0; j < count; j++)
                            entry.Values.Add((32, pkt.Read<uint>(32)));

                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        break;
                    case 28:
                        count = pkt.Read<uint>(8);

                        entry.Values.Add((8, count));

                        for (var j = 0; j < count; j++)
                            entry.Values.Add((32, pkt.Read<uint>(32)));

                        for (var j = 0; j < count; j++)
                            entry.Values.Add((32, pkt.Read<uint>(32)));

                        entry.Values.Add((32, pkt.Read<uint>(32)));
                        break;
                    default:
                        throw new NotImplementedException($"commandType {commandType} not implemented.");
                }

                values.Add(entry);
            }
        }
    }
}
