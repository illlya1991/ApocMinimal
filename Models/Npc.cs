using System.Text.Json.Serialization;

namespace ApocMinimal.Models;

public enum NpcTrait
{
    None,    // Обычный
    Leader,  // Лидер: +3 Faith соседям каждый день
    Coward,  // Трус: 50% шанс отказа от задания
    Loner    // Одиночка: иммунен к бонусу Лидера
}

public enum Gender { Male, Female}

/// <summary>
/// Full NPC model (Stage A+).
/// </summary>
public class Npc
{
    // ── IDENTITY ────────────────────────────────────────────────────────────
    public int      Id         { get; set; }
    public string   Name       { get; set; } = "";
    public int      Age        { get; set; }
    public Gender   Gender     { get; set; }
    public string   Profession { get; set; } = "";
    public string   Description { get; set; } = "";

    // ── VITALS ──────────────────────────────────────────────────────────────
    public double Health   { get; set; } = 100;
    public double Faith    { get; set; }
    public double Stamina  { get; set; } = 100;  // 0–100, restored each day
    public double Chakra   { get; set; } = 50;   // 0–100, energy resource

    /// <summary>0–100: how much this NPC fears the current situation.</summary>
    public double Fear    { get; set; } = 10;
    /// <summary>0–100: trust in the player (divine entity).</summary>
    public double Trust   { get; set; } = 50;
    /// <summary>0–100: initiative (chance to act first, take quests proactively).</summary>
    public double Initiative { get; set; } = 50;
    /// <summary>0–100: combat initiative (determines attack order in battle).</summary>
    public double CombatInitiative { get; set; } = 50;

    // ── ROLE & PROGRESSION ──────────────────────────────────────────────────
    public NpcTrait Trait          { get; set; }
    public int      FollowerLevel  { get; set; }  // 0–5

    /// <summary>2 character traits.</summary>
    public List<CharacterTrait> CharTraits { get; set; } = new();

    /// <summary>Up to 50 specializations (skill names).</summary>
    public List<string> Specializations { get; set; } = new();

    // ── PSYCHOLOGY ──────────────────────────────────────────────────────────
    /// <summary>Exactly 3 emotions summing to 100%.</summary>
    public List<Emotion> Emotions { get; set; } = new();

    public string Goal   { get; set; } = "";  // короткосрочная цель
    public string Dream  { get; set; } = "";  // долгосрочная мечта
    public string Desire { get; set; } = "";  // текущее желание

    // ── NEEDS ───────────────────────────────────────────────────────────────
    /// <summary>10 basic + up to 10 special needs.</summary>
    public List<Need> Needs { get; set; } = new();

    // ── STATS ───────────────────────────────────────────────────────────────
    /// <summary>30 characteristics keyed by StatDefs ID (1–30), values 0–100.</summary>
    public Dictionary<int, double> Stats { get; set; } = new();

    // ── TASK (legacy, kept for DB compatibility) ─────────────────────────────
    public string ActiveTask      { get; set; } = "";
    public int    TaskDaysLeft    { get; set; }
    public int    TaskRewardResId { get; set; }
    public double TaskRewardAmt   { get; set; }

    // ── MEMORY ──────────────────────────────────────────────────────────────
    /// <summary>Last 50 memory entries (serialised separately).</summary>
    [JsonIgnore]
    public List<MemoryEntry> Memory { get; set; } = new();

    // ── COMPUTED ─────────────────────────────────────────────────────────────
    public bool IsAlive => Health > 0;
    public bool HasTask => !string.IsNullOrEmpty(ActiveTask);

    // Hunger/Thirst are now Needs; these helpers find them.
    [JsonIgnore] public double Hunger => Needs.FirstOrDefault(n => n.Name == "Еда")?.Value   ?? 0;
    [JsonIgnore] public double Thirst => Needs.FirstOrDefault(n => n.Name == "Вода")?.Value  ?? 0;

    public string TraitLabel => Trait switch
    {
        NpcTrait.Leader => "Лидер",
        NpcTrait.Coward => "Трус",
        NpcTrait.Loner  => "Одиночка",
        _               => ""
    };

    public string GenderLabel => Gender switch
    {
        Gender.Male   => "М",
        Gender.Female => "Ж"
    };

    public string FollowerLabel => FollowerLevel switch
    {
        0 => "Нейтральный",
        1 => "Послушник",
        2 => "Последователь",
        3 => "Верный",
        4 => "Преданный",
        5 => "Фанатик",
        _ => FollowerLevel.ToString()
    };

    public string StatusColor
    {
        get
        {
            if (!IsAlive)              return "#3d1515";
            if (Health < 30)           return "#3d2200";
            if (Hunger > 80 || Thirst > 80) return "#3d3200";
            return "#16213e";
        }
    }

    /// <summary>
    /// Adds a memory entry, keeping at most 50 entries.
    /// </summary>
    public void Remember(MemoryEntry entry)
    {
        Memory.Add(entry);
        if (Memory.Count > 50)
            Memory.RemoveAt(0);
    }
}

// ── STATIC TABLES ────────────────────────────────────────────────────────────

public static class NpcGoals
{
    public static readonly string[] Goals =
    {
        "Найти безопасное место", "Обеспечить группу едой", "Починить транспорт",
        "Вылечить больного", "Укрепить базу", "Найти семью", "Связаться с внешним миром",
        "Освоить новый навык", "Найти оружие", "Разведать карту",
    };
    public static readonly string[] Dreams =
    {
        "Восстановить цивилизацию", "Найти рай на земле", "Создать собственное поселение",
        "Стать лидером клана", "Записать историю апокалипсиса", "Вернуться домой",
        "Найти лекарство от болезни", "Объединить выживших", "Построить ферму",
        "Достичь просветления",
    };
    public static readonly string[] Desires =
    {
        "Нормально поесть", "Выспаться", "Поговорить с кем-нибудь",
        "Помыться", "Послушать музыку", "Почувствовать тепло",
        "Прочитать книгу", "Выпить алкоголь", "Уединиться",
        "Узнать новости",
    };
}

public static class NpcDescriptions
{
    public static readonly string[] All =
    {
        "Молчаливый и наблюдательный. Редко говорит, но каждое слово весомо.",
        "Оптимист по натуре. Всегда ищет лучшее в людях и ситуациях.",
        "Бывший военный. Дисциплинирован, требователен к себе и другим.",
        "Нервный и тревожный. Постоянно оглядывается через плечо.",
        "Харизматичный. Легко завоёвывает доверие окружающих.",
        "Замкнутый. Не любит делиться информацией о своём прошлом.",
        "Прагматик. Оценивает всё через призму пользы и выживания.",
        "Добросердечный. Готов отдать последнее ради другого.",
        "Циничный. Не верит в альтруизм и видит расчёт в каждом поступке.",
        "Загадочный. Кажется, знает больше, чем говорит.",
    };
}
