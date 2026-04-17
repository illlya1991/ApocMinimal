namespace ApocMinimal.Models.PersonData.NpcData;

/// <summary>A single resource item in an NPC's personal inventory.</summary>
public class NpcInventoryItem
{
    public int Id { get; set; }
    public int NpcId { get; set; }
    public string ResourceName { get; set; } = "";
    public double Amount { get; set; }

    public override string ToString() => $"{ResourceName} ×{Amount:F0}";
}
