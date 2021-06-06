using System;

[Serializable()]public class CharacterCustomization
{
    public uint Id { get; set; }
    public uint RaceId { get; set; }
    public uint Gender { get; set; }
    public uint ItemSlotId { get; set; }
    public uint ItemDisplayId { get; set; }
    public uint Flags { get; set; }
    public uint CharacterCustomizationLabelId00 { get; set; }
    public uint CharacterCustomizationLabelId01 { get; set; }
    public uint Value00 { get; set; }
    public uint Value01 { get; set; }
}
