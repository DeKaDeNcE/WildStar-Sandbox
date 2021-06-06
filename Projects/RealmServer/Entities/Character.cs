// Copyright (c) Arctium.

using System;
using System.Collections.Generic;
using System.Numerics;

namespace RealmServer.Entities
{
    [Serializable()]
    public class Character
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public uint Sex { get; set; }
        public uint Race { get; set; }
        public uint Class { get; set; }
        public uint Faction { get; set; }
        public uint DisplayInfoId { get; set; }
        public uint Level { get; set; }
        public byte Path { get; set; }
        public List<CharacterCustomization> Customizations { get; set; }
        public Vector3 Location { get; set; }
        public uint WorldId { get; set; }
        public uint AccountId { get; } = 1;

        public float MoveSpeed { get; set; }
        public float JumpHeight { get; set; }
        public float GravityMultiplier { get; set; }

        // Temp
        public uint[] BoneCustomizations { get; set; }
        public List<uint[]> EquipmentVisuals { get; set; }

        public bool SheathState { get; set; }
    }
}
