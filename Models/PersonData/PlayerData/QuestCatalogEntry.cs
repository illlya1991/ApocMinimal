namespace ApocMinimal.Models.PersonData.PlayerData;

public enum QuestType { OneTime, Repeatable, Eternal }

public class QuestCatalogEntry
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public double OvCost { get; set; }
    public int MinAltarLevel { get; set; } = 1;
    public QuestType QuestType { get; set; }
    public int MaxRepeats { get; set; } = 1;
    public int DaysRequired { get; set; } = 3;
    public string RewardResource { get; set; } = "";
    public double RewardAmount { get; set; }
    public string Category { get; set; } = "";

    public string QuestTypeLabel => QuestType switch
    {
        QuestType.OneTime => "Одноразовый",
        QuestType.Repeatable => $"×{MaxRepeats}",
        QuestType.Eternal => "Вечный",
        _ => ""
    };
}
