public class SpellLevel
{
    public uint Id { get; set; }
    public uint ClassId { get; set; }
    public uint CharacterLevel { get; set; }
    public uint PrerequisiteId { get; set; }
    public uint Spell4Id { get; set; }
    public float CostMultiplier { get; set; }
}
