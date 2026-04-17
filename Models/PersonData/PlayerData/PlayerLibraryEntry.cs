namespace ApocMinimal.Models.PersonData.PlayerData;

public class PlayerLibraryEntry
{
    public int Id { get; set; }
    public string SaveId { get; set; } = "";
    public int CatalogId { get; set; }
    public int PublishesLeft { get; set; }
    public int TimesCompleted { get; set; }
    public QuestCatalogEntry? Catalog { get; set; }

    public bool CanPublish => PublishesLeft == -1 || PublishesLeft > 0;
    public string PublishLabel => PublishesLeft == -1 ? "∞" : PublishesLeft.ToString();
}
