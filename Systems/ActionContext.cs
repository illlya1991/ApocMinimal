using ApocMinimal.Models.LocationData;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.ResourceData;

namespace ApocMinimal.Systems;

public class ActionContext
{
    public List<Resource> Resources { get; set; } = new();
    public List<Location> Locations { get; set; } = new();
    /// <summary>All NPCs — used for social action pairing.</summary>
    public List<Npc> Npcs { get; set; } = new();
    /// <summary>External log entries injected by other NPCs (social pairings). Key = NPC id.</summary>
    public Dictionary<int, List<ActionLogEntry>> NpcLogs { get; set; } = new();
    /// <summary>NPC ids that have already been paired for a social action today.</summary>
    public HashSet<int> SocialPairedToday { get; set; } = new();
}
