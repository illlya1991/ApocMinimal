namespace ApocMinimal.Models.PersonData.PlayerData;

public class QuestHistoryEntry
{
    public int Id { get; set; }
    public string SaveId { get; set; } = "";
    public int CatalogId { get; set; }
    public string QuestTitle { get; set; } = "";
    public string NpcName { get; set; } = "";
    public int DayTaken { get; set; }
    public int DayCompleted { get; set; }
    public string RewardGiven { get; set; } = "";
}
