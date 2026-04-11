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

    /// <summary>ID of NPC assigned to this quest (0 = unassigned).</summary>
    public int AssignedNpcId { get; set; }

    public int DaysRequired { get; set; } = 3;
    public int DaysRemaining { get; set; }
    public int RewardResourceId { get; set; }
    public double RewardAmount { get; set; }

    /// <summary>Faith cost for player to unlock this quest (0 = free).</summary>
    public double FaithCost { get; set; }

    /// <summary>Required minimum NPC stat for auto-assignment (StatId → minValue, 0 = any).</summary>
    public Dictionary<int, double> RequiredStats { get; set; } = new();
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
