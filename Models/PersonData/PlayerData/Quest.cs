namespace ApocMinimal.Models.PersonData.PlayerData;

public enum QuestStatus { Available, Active, Completed, Failed }
public enum QuestSource { Player, AI }

public class Quest
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public QuestSource Source { get; set; }
    public QuestStatus Status { get; set; } = QuestStatus.Available;

    public int AssignedNpcId { get; set; }

    public int DaysRequired { get; set; } = 3;
    public int DaysRemaining { get; set; }
    public int RewardResourceId { get; set; }
    public double RewardAmount { get; set; }

    public double FaithCost { get; set; }

    public Dictionary<int, double> RequiredStats { get; set; } = new();

    public QuestType QuestType { get; set; } = QuestType.OneTime;
    public int LibraryId { get; set; } = 0;
    public CompleteType CompleteType { get; set; } = CompleteType.Time;
    public double CompleteProgress { get; set; } = 0;
    public double CompleteTarget { get; set; } = 0;
    public int DayTaken { get; set; } = 0;
    public RewardType RewardType { get; set; } = RewardType.Resource;
    public string RewardTechnique { get; set; } = "";

    public double CompletionPercent => CompleteType == CompleteType.Time
        ? (DaysRequired > 0 ? (1.0 - (double)DaysRemaining / DaysRequired) * 100 : 0)
        : (CompleteTarget > 0 ? CompleteProgress / CompleteTarget * 100 : 0);
}

public static class QuestTemplates
{
    public static readonly (string Title, string Desc, int Days, int ResId, double Reward, double FaithCost)[] All =
    {
        ("Добыть продовольствие",   "Найти запасы еды в ближайших районах.",          3, 1, 20, 0),
        ("Запасти воду",            "Собрать чистую воду из нескольких источников.",   2, 2, 25, 0),
        ("Разведать район",         "Изучить соседнюю территорию на предмет угроз.",   2, 0,  0, 5),
        ("Построить укрепление",    "Возвести баррикаду вокруг базы.",                 5, 3, 10, 10),
        ("Найти медикаменты",       "Обыскать аптеку или больницу.",                   3, 3, 15, 0),
        ("Починить генератор",      "Восстановить электроснабжение базы.",             4, 5,  5, 15),
        ("Отогнать мародёров",      "Защитить территорию от нападения группы.",        1, 0,  0, 20),
        ("Собрать информацию",      "Узнать о соседних группах выживших.",             3, 0,  0, 10),
        ("Заготовить дрова",        "Принести достаточно дров для обогрева.",          2, 4, 20, 0),
        ("Установить наблюдение",   "Расставить сигнальные ловушки по периметру.",     3, 5,  3, 5),
        ("Найти артефакт",          "Отыскать ценный предмет старого мира.",           5, 0,  0, 30),
        ("Переговоры с соседями",   "Заключить перемирие с другой группой.",           4, 0,  0, 15),
    };
}
