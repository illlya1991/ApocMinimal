namespace ApocMinimal.Models.PersonData.PlayerData;

public enum QuestType { OneTime, Repeatable, Eternal }
public enum CompleteType { Time, Resource, Action }
public enum RewardType { Resource, Technique, DevPoints }

public class QuestCatalogEntry
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int MinTerminalLevel { get; set; } = 1;

    public double? PriceOneTime { get; set; }
    public double? PriceRepeatable { get; set; }
    public double? PriceEternal { get; set; }

    public string TakeCondStat { get; set; } = "";
    public int TakeCondValue { get; set; } = 0;

    public CompleteType CompleteType { get; set; } = CompleteType.Time;
    public int CompleteDays { get; set; } = 3;
    public string CompleteResource { get; set; } = "";
    public double CompleteAmount { get; set; } = 0;
    public string CompleteAction { get; set; } = "";

    public RewardType RewardType { get; set; } = RewardType.Resource;
    public string RewardResource { get; set; } = "";
    public double RewardAmount { get; set; } = 0;
    public string RewardTechnique { get; set; } = "";

    public string Category { get; set; } = "";

    public string TakeCondLabel => string.IsNullOrEmpty(TakeCondStat) ? "—"
        : $"{TakeCondStatLabel} ≥ {TakeCondValue}";

    public string TakeCondStatLabel => TakeCondStat switch
    {
        "Initiative" => "Инициатива",
        "Faith" => "Преданность",
        "Trust" => "Доверие",
        "Fear" => "Страх ≤",
        "FollowerLevel" => "Уровень",
        "strength" => "Сила",
        "endurance" => "Выносливость",
        "intelligence" => "Интеллект",
        "reflexes" => "Рефлексы",
        "sensorics" => "Сенсорика",
        "social_intel" => "Соц.интеллект",
        "learning" => "Обучение",
        _ => TakeCondStat
    };

    public string CompletionLabel => CompleteType switch
    {
        CompleteType.Time => $"{CompleteDays} дн.",
        CompleteType.Resource => $"{CompleteAmount:F0} {CompleteResource}",
        CompleteType.Action => $"{CompleteAmount:F0}× {CompleteAction}",
        _ => ""
    };

    public string RewardLabel => RewardType switch
    {
        RewardType.Resource => RewardAmount > 0 ? $"{RewardAmount:F0} {RewardResource}" : "—",
        RewardType.DevPoints => $"{RewardAmount:F0} ОР",
        RewardType.Technique => string.IsNullOrEmpty(RewardTechnique) ? "Техника" : RewardTechnique,
        _ => "—"
    };
}
