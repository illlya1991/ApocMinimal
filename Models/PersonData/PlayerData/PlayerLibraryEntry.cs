namespace ApocMinimal.Models.PersonData.PlayerData;

public class PlayerLibraryEntry
{
    public int Id { get; set; }
    public string SaveId { get; set; } = "";
    public int CatalogId { get; set; }
    public int PublishesLeft { get; set; }
    public int TimesCompleted { get; set; }
    public QuestType QuestType { get; set; } = QuestType.OneTime;
    public QuestCatalogEntry? Catalog { get; set; }

    public bool CanPublish => PublishesLeft == -1 || PublishesLeft > 0;
    public string PublishLabel => PublishesLeft == -1 ? "∞" : PublishesLeft.ToString();

    public string RewardSummary
    {
        get
        {
            if (Catalog == null) return "—";
            return Catalog.RewardType == RewardType.Faith
                ? $"{Catalog.RewardAmount:F0} ОР"
                : "другое";
        }
    }
}
