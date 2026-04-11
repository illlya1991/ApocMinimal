namespace ApocMinimal.Models.PersonData.NpcData;

public enum MemoryType
{
    Action,    // NPC performed an action
    Quest,     // quest started / completed / failed
    Combat,    // participated in combat
    Social,    // interaction with another NPC
    Discovery, // found a location or resource
    Divine,    // received a divine intervention from the player
    StatChange, // a stat value changed significantly
}

/// <summary>
/// A single memory record stored per NPC. NPC remembers last 50 entries.
/// </summary>
public class MemoryEntry
{
    public int Day { get; set; }
    public MemoryType Type { get; set; }
    public string Text { get; set; } = "";
    public int NpcId { get; set; }  // 0 = self

    public MemoryEntry() { }
    public MemoryEntry(int day, MemoryType type, string text, int npcId = 0)
    {
        Day = day; Type = type; Text = text; NpcId = npcId;
    }

    public string Icon => Type switch
    {
        MemoryType.Action => "⚡",
        MemoryType.Quest => "📋",
        MemoryType.Combat => "⚔",
        MemoryType.Social => "💬",
        MemoryType.Discovery => "🔍",
        MemoryType.Divine => "✦",
        MemoryType.StatChange => "📈",
        _ => "•",
    };
}
