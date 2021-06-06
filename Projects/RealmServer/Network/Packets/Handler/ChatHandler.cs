// Copyright (c) Arctium.

using RealmServer.Attributes;
using RealmServer.Constants.Net;

using System;
using System.Linq;
using System.Numerics;

using static RealmServer.Managers.Manager;

namespace RealmServer.Network.Packets.Handler
{
    class ChatHandler
    {
        [RealmPacket(ClientMessage.ChatMessage)]
        public static void HandleChatMessage(Packet packet, RealmSession session)
        {
            var channelId = packet.Read<ushort>(14);
            var chatId = packet.Read<ulong>(64);

            // message
            var text = packet.ReadWString();
            Console.WriteLine(text);
            var formatCount = packet.Read<byte>(5);

            if (formatCount > 0)
            {

                for (var i = 0; i < formatCount; i++)
                {
                    var type = packet.Read<byte>(4);
                    var startIndex = packet.Read<ushort>(16);
                    var stopIndex = packet.Read<ushort>(16);

                    switch (type)
                    {
                        case 0:
                        case 2:
                        case 3:
                            var unk = packet.Read<bool>(1);
                            break;
                        case 1:
                        case 7:
                        case 11:
                            var unk2 = packet.Read<uint>(32);
                            break;
                        case 4:
                            var unk3 = packet.Read<uint>(18);
                            break;
                        case 5:
                            var unk4 = packet.Read<uint>(15);
                            break;
                        case 6:
                            var unk5 = packet.Read<uint>(14);
                            break;
                        case 8:
                            throw new NotSupportedException("type == 8");
                        case 9:
                            var unk6 = packet.Read<ulong>(64);
                            break;
                        case 10:
                            throw new NotSupportedException("type == 8");
                        default:
                            break;
                    }
                }
            }
            else
            {
                var unk = packet.Read<ushort>(16);
            }


            if (text.StartsWith("!"))
            {
                var line = text.Split(new[] { " " }, StringSplitOptions.None);

                if (line.Length == 0)
                    return;

                var cmd = line[0].Remove(0, 1).Trim();

                if (cmd == "tele" && line.Length >= 4)
                {
                    session.Character.Location = new Vector3
                    {
                        X = Convert.ToSingle(line[1].Replace(",", ".").Trim()),
                        Y = Convert.ToSingle(line[2].Replace(",", ".").Trim()),
                        Z = Convert.ToSingle(line[3].Replace(",", ".").Trim())
                    };

                    if (line.Length > 4)
                        session.Character.WorldId = Convert.ToUInt16(line[4].Trim());

                    DataMgr.UpdateCharacter(session.Character);


                    GatewayHandler.LoginToWorld(session.Character, session);
                }
                else if (cmd == "pos")
                {
                    var newPkt = new Packet((ServerMessage)0x638);

                    newPkt.Write(283041, 32);
                    newPkt.Write(1, 32);
                    newPkt.Write(false, 1);
                    newPkt.Write(true, 1);

                    newPkt.Write(1, 5);

                    newPkt.Write(2, 5);

                    newPkt.WriteFloat(session.Character.Location.X, 32);
                    newPkt.WriteFloat(session.Character.Location.Y + 0.5f, 32);
                    newPkt.WriteFloat(session.Character.Location.Z, 32);
                    newPkt.Write(false, 1);
                    session.Send(newPkt);
                }
                else if (cmd == "movespeed" && line.Length == 2)
                {
                    var speedMultiplier = Convert.ToSingle(line[1].Replace(",", ".").Trim());

                    var updateproperty = new Packet(ServerMessage.UpdateUnitProperties);
                    updateproperty.Write(283041, 32); // unit id
                    updateproperty.Write(1, 8);

                    updateproperty.Write(100, 8); // MoveSpeedMultiplier
                    updateproperty.WriteFloat(speedMultiplier, 32);
                    updateproperty.WriteFloat(speedMultiplier, 32);

                    session.Character.MoveSpeed = speedMultiplier;

                    DataMgr.UpdateCharacter(session.Character);

                    session.Send(updateproperty);
                }
                else if (cmd == "jumpheight" && line.Length == 2)
                {
                    var jumpHeight = Convert.ToSingle(line[1].Replace(",", ".").Trim());

                    var updateproperty = new Packet(ServerMessage.UpdateUnitProperties);
                    updateproperty.Write(283041, 32); // unit id
                    updateproperty.Write(1, 8);

                    updateproperty.Write(129, 8); // JumpHeight
                    updateproperty.WriteFloat(jumpHeight, 32);
                    updateproperty.WriteFloat(jumpHeight, 32);

                    session.Character.JumpHeight = jumpHeight;

                    DataMgr.UpdateCharacter(session.Character);

                    session.Send(updateproperty);
                }
                else if (cmd == "gravity" && line.Length == 2)
                {
                    var gravity = Convert.ToSingle(line[1].Replace(",", ".").Trim());

                    var updateproperty = new Packet(ServerMessage.UpdateUnitProperties);
                    updateproperty.Write(283041, 32); // unit id
                    updateproperty.Write(1, 8);

                    updateproperty.Write(130, 8); // GravityMultiplier
                    updateproperty.WriteFloat(gravity, 32);
                    updateproperty.WriteFloat(gravity, 32);

                    session.Character.GravityMultiplier = gravity;

                    DataMgr.UpdateCharacter(session.Character);

                    session.Send(updateproperty);
                }
                else if (cmd == "time")
                {
                    var hour = int.Parse(line[1].Trim());
                    var updateTime = new Packet(ServerMessage.Time);

                    updateTime.Write(hour * 60 * 60, 32);
                    updateTime.Write(0, 32);
                    updateTime.Write(86400, 32);

                    session.Send(updateTime);
                }
                else if (cmd == "morph" && line.Length == 2)
                {
                    session.Character.DisplayInfoId = Convert.ToUInt32(line[1].Trim());

                    GatewayHandler.LoginToWorld(session.Character, session);
                }
                else if (cmd == "clearequip" && line.Length >= 1)
                {
                    var updateEquipment = new Packet(ServerMessage.UpdateEquipment);

                    updateEquipment.Write(283041, 32); // unit id
                    updateEquipment.Write(session.Character.EquipmentVisuals.Count, 32); // count


                    for (var i = 0; i < session.Character.EquipmentVisuals.Count; i++)
                    {
                        var equip = session.Character.EquipmentVisuals[i];

                        updateEquipment.Write(equip[0], 7);
                        updateEquipment.Write(0, 0xF);
                        updateEquipment.Write(0, 0xE);
                        updateEquipment.Write(0, 0x20);
                    }

                    DataMgr.UpdateCharacter(session.Character);

                    session.Send(updateEquipment);
                }
                else if (cmd == "clearbody" && line.Length >= 1)
                {
                    var updateEquipment = new Packet(ServerMessage.UpdateEquipment);

                    updateEquipment.Write(283041, 32); // unit id
                    updateEquipment.Write(session.Character.Customizations.Count, 32); // count


                    foreach (var bv in session.Character.Customizations)
                    {
                        updateEquipment.Write(bv.ItemSlotId, 7);
                        updateEquipment.Write(0, 0xF);
                        updateEquipment.Write(0, 0xE);
                        updateEquipment.Write(0, 0x20);
                    }

                    DataMgr.UpdateCharacter(session.Character);

                    session.Send(updateEquipment);
                }
                else if (cmd == "clearslot" && line.Length >= 2)
                {
                    var slot = Convert.ToInt32(line[1].Trim());
                    var updateEquipment = new Packet(ServerMessage.UpdateEquipment);

                    updateEquipment.Write(283041, 32); // unit id
                    updateEquipment.Write(1, 32); // count


                    updateEquipment.Write(slot, 7);
                    updateEquipment.Write(0, 0xF);
                    updateEquipment.Write(0, 0xE);
                    updateEquipment.Write(0, 0x20);

                    DataMgr.UpdateCharacter(session.Character);

                    session.Send(updateEquipment);
                }
                else if (cmd == "visual" && line.Length >= 2)
                {
                    //var slot = Convert.ToInt32(line[1].Trim());
                    var displayId = Convert.ToUInt32(line[1].Trim());
                    var colorSetId = 0u;
                    var dyeData = 0u;

                    var updateEquipment = new Packet(ServerMessage.UpdateEquipment);

                    updateEquipment.Write(283041, 32); // unit id
                    updateEquipment.Write(1, 32); // count

                    var item = TableMgr.ItemDisplay.SingleOrDefault(id => id.Id == displayId);

                    if (item == null)
                        return;

                    colorSetId = item.ItemColorSetId;

                    var slot = TableMgr.Item2Type.Single(i2t => i2t.Id == item.Item2TypeId).ItemSlotId;
                    var itemslot = TableMgr.ItemSlot.Single(its => its.Id == slot);

                    if (line.Length == 3 || line.Length == 6)
                        colorSetId = Convert.ToUInt32(line[2].Trim());

                    if (line.Length == 5)
                    {
                        var dye0 = TableMgr.DyeColorRamps.SingleOrDefault(dcr => dcr.Id == Convert.ToUInt32(line[2]))?.RampIndex ?? 0u;
                        var dye1 = TableMgr.DyeColorRamps.SingleOrDefault(dcr => dcr.Id == Convert.ToUInt32(line[3]))?.RampIndex ?? 0u;
                        var dye2 = TableMgr.DyeColorRamps.SingleOrDefault(dcr => dcr.Id == Convert.ToUInt32(line[4]))?.RampIndex ?? 0u;

                        dyeData = dye0 & 0x3FF | ((dye1 & 0x3FF | ((dye2 & 0x3FF | 0xFFFFF800) << 10)) << 10);
                    }
                    else if (line.Length == 6)
                    {
                        var dye0 = TableMgr.DyeColorRamps.SingleOrDefault(dcr => dcr.Id == Convert.ToUInt32(line[3]))?.RampIndex ?? 0u;
                        var dye1 = TableMgr.DyeColorRamps.SingleOrDefault(dcr => dcr.Id == Convert.ToUInt32(line[4]))?.RampIndex ?? 0u;
                        var dye2 = TableMgr.DyeColorRamps.SingleOrDefault(dcr => dcr.Id == Convert.ToUInt32(line[5]))?.RampIndex ?? 0u;

                        dyeData = dye0 & 0x3FF | ((dye1 & 0x3FF | ((dye2 & 0x3FF | 0xFFFFF800) << 10)) << 10);
                    }

                    updateEquipment.Write(slot, 7);
                    updateEquipment.Write(displayId, 0xF);
                    updateEquipment.Write(colorSetId, 0xE);
                    updateEquipment.Write(dyeData, 0x20);

                    if (itemslot.EquippedSlotFlags != 0)
                    {
                        var found = false;
                        // Gear
                        for (var i = 0; i < session.Character.EquipmentVisuals.ToList().Count; i++)
                        {
                            if (session.Character.EquipmentVisuals[i][0] == slot)
                            {
                                found = true;
                                session.Character.EquipmentVisuals[i] = new[] { slot, displayId, colorSetId, dyeData };
                                break;
                            }
                        }

                        if (!found)
                            session.Character.EquipmentVisuals.Add(new[] { slot, displayId, colorSetId, dyeData });
                    }
                    else
                    {
                        var found = false;

                        for (var i = 0; i < session.Character.Customizations.ToList().Count; i++)
                        {
                            if (session.Character.Customizations[i].ItemSlotId == slot)
                            {
                                found = true;

                                session.Character.Customizations[i].ItemSlotId = (uint)slot;
                                session.Character.Customizations[i].ItemDisplayId = (uint)displayId;
                                break;
                            }
                        }

                        if (!found)
                        {
                            session.Character.Customizations.Add(new CharacterCustomization
                            {
                                Id = 0,
                                ItemSlotId = (uint)slot,
                                ItemDisplayId = (uint)displayId
                            });
                        }
                    }

                    DataMgr.UpdateCharacter(session.Character);

                    session.Send(updateEquipment);
                }
                else if (cmd == "item" && line.Length >= 2)
                {
                    //var slot = Convert.ToInt32(line[1].Trim());
                    var itemId = Convert.ToInt32(line[1].Trim());
                    var colorSetId = 0u;
                    var dyeData = 0u;

                    var updateEquipment = new Packet(ServerMessage.UpdateEquipment);

                    updateEquipment.Write(283041, 32); // unit id
                    updateEquipment.Write(1, 32); // count

                    var item = TableMgr.Item2.SingleOrDefault(id => id.Id == itemId);

                    if (item == null)
                        return;

                    colorSetId = item.ItemColorSetId;

                    var slot = TableMgr.Item2Type.Single(i2t => i2t.Id == item.Item2TypeId).ItemSlotId;
                    var itemslot = TableMgr.ItemSlot.Single(its => its.Id == slot);

                    if (line.Length == 3 || line.Length == 6)
                        colorSetId = Convert.ToUInt32(line[2].Trim());

                    if (line.Length == 5)
                    {
                        var dye0 = TableMgr.DyeColorRamps.SingleOrDefault(dcr => dcr.Id == Convert.ToUInt32(line[2]))?.RampIndex ?? 0u;
                        var dye1 = TableMgr.DyeColorRamps.SingleOrDefault(dcr => dcr.Id == Convert.ToUInt32(line[3]))?.RampIndex ?? 0u;
                        var dye2 = TableMgr.DyeColorRamps.SingleOrDefault(dcr => dcr.Id == Convert.ToUInt32(line[4]))?.RampIndex ?? 0u;

                        dyeData = dye0 & 0x3FF | ((dye1 & 0x3FF | ((dye2 & 0x3FF | 0xFFFFF800) << 10)) << 10);
                    }
                    else if (line.Length == 6)
                    {
                        var dye0 = TableMgr.DyeColorRamps.SingleOrDefault(dcr => dcr.Id == Convert.ToUInt32(line[3]))?.RampIndex ?? 0u;
                        var dye1 = TableMgr.DyeColorRamps.SingleOrDefault(dcr => dcr.Id == Convert.ToUInt32(line[4]))?.RampIndex ?? 0u;
                        var dye2 = TableMgr.DyeColorRamps.SingleOrDefault(dcr => dcr.Id == Convert.ToUInt32(line[5]))?.RampIndex ?? 0u;

                        dyeData = dye0 & 0x3FF | ((dye1 & 0x3FF | ((dye2 & 0x3FF | 0xFFFFF800) << 10)) << 10);
                    }

                    updateEquipment.Write(slot, 7);
                    updateEquipment.Write(item.ItemDisplayId, 0xF);
                    updateEquipment.Write(colorSetId, 0xE);
                    updateEquipment.Write(dyeData, 0x20);

                    if (itemslot.EquippedSlotFlags != 0)
                    {
                        var found = false;
                        // Gear
                        for (var i = 0; i < session.Character.EquipmentVisuals.ToList().Count; i++)
                        {
                            if (session.Character.EquipmentVisuals[i][0] == slot)
                            {
                                found = true;
                                session.Character.EquipmentVisuals[i] = new[] { slot, item.ItemDisplayId, colorSetId, dyeData };
                                break;
                            }
                        }

                        if (!found)
                            session.Character.EquipmentVisuals.Add(new[] { slot, item.ItemDisplayId, colorSetId, dyeData });
                    }
                    else
                    {
                        var found = false;

                        for (var i = 0; i < session.Character.Customizations.ToList().Count; i++)
                        {
                            if (session.Character.Customizations[i].ItemSlotId == slot)
                            {
                                found = true;

                                session.Character.Customizations[i].ItemSlotId = (uint)slot;
                                session.Character.Customizations[i].ItemDisplayId = item.ItemDisplayId;
                                break;
                            }
                        }

                        if (!found)
                        {
                            session.Character.Customizations.Add(new CharacterCustomization
                            {
                                Id = 0,
                                ItemSlotId = (uint)slot,
                                ItemDisplayId = item.ItemDisplayId
                            });
                        }
                    }

                    DataMgr.UpdateCharacter(session.Character);

                    session.Send(updateEquipment);
                }
                else
                    Console.WriteLine("Received invalid chat command.");
            }
        }
    }
}
