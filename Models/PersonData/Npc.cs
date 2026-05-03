using ApocMinimal.Models.StatisticsData;
using ApocMinimal.Models.PersonData.NpcData;
using System.Text.Json.Serialization;

namespace ApocMinimal.Models.PersonData;

public enum NpcTrait
{
    None,    // Обычный
    Leader,  // Лидер: +3 Devotion соседям каждый день
    Coward,  // Трус: 50% шанс отказа от задания
    Loner    // Одиночка: иммунен к бонусу Лидера
}

public enum Gender { Male, Female }

/// <summary>
/// Full NPC model (Stage A+).
/// </summary>
public class Npc
{
    // ── IDENTITY ────────────────────────────────────────────────────────────
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public Gender Gender { get; set; }
    public string Profession { get; set; } = "";
    public string Description { get; set; } = "";

    // ── VITALS ──────────────────────────────────────────────────────────────
    public double Health { get; set; } = 100;
    public double Devotion { get; set; }
    public double Stamina { get; set; } = 100;
    public double Energy { get; set; } = 50;
    public double Fear { get; set; } = 10;
    public double Trust { get; set; } = 50;
    public double Initiative { get; set; } = 50;
    public double CombatInitiative { get; set; } = 50;

    // ── ROLE & PROGRESSION ──────────────────────────────────────────────────
    public NpcTrait Trait { get; set; }
    public int FollowerLevel { get; set; }
    public int EvolutionLevel { get; set; }
    public List<CharacterTrait> CharTraits { get; set; } = new();
    public List<string> Specializations { get; set; } = new();

    // ── PSYCHOLOGY ──────────────────────────────────────────────────────────
    public List<Emotion> Emotions { get; set; } = new();
    public string Goal { get; set; } = "";
    public string Dream { get; set; } = "";
    public string Desire { get; set; } = "";

    // ── NEEDS ───────────────────────────────────────────────────────────────
    public List<Need> Needs { get; set; } = new();

    // ── STATS ───────────────────────────────────────────────────────────────
    public Statistics Stats { get; set; } = new Statistics(100);

    // ── TASK ─────────────────────────────────────────────────────────────────
    public string ActiveTask { get; set; } = "";
    public int TaskDaysLeft { get; set; }
    public int TaskRewardResId { get; set; }
    public double TaskRewardAmt { get; set; }

    // ── INVENTORY ───────────────────────────────────────────────────────────
    public List<NpcInventoryItem> Inventory { get; set; } = new();

    // ── INJURIES ────────────────────────────────────────────────────────────
    public List<Injury> Injuries { get; set; } = new();

    // ── LEARNED TECHNIQUES ─────────────────────────────────────────────────
    public List<string> LearnedTechIds { get; set; } = new();

    // ── LOCATION ─────────────────────────────────────────────────────────────
    /// <summary>Current location Id. 0 = home base (community).</summary>
    public int LocationId { get; set; } = 0;

    // ── MEMORY ──────────────────────────────────────────────────────────────
    [JsonIgnore]
    public List<MemoryEntry> Memory { get; set; } = new();

    // ── COMPUTED ─────────────────────────────────────────────────────────────
    public bool IsAlive => Health > 0;
    public bool HasTask => !string.IsNullOrEmpty(ActiveTask);

    /// <summary>MaxEnergy = (EnergyReserve×6 + Concentration×3 + Control×2) / 11. Base 100 stats → 100.</summary>
    public double MaxEnergy => (Stats.EnergyReserve * 6 + Stats.Concentration * 3 + Stats.Control * 2) / 11.0;

    /// <summary>MaxStamina = (Endurance×5 + Toughness×3 + Strength×2) / 10. Stats=100 → 100.</summary>
    public double MaxStamina => (Stats.Endurance * 5 + Stats.Toughness * 3 + Stats.Strength * 2) / 10.0;

    [JsonIgnore]
    public double Hunger
    {
        get
        {
            for (int i = 0; i < Needs.Count; i++)
                if (Needs[i].Id == (int)BasicNeedId.Food) return Needs[i].Value;
            return 0;
        }
    }

    [JsonIgnore]
    public double Thirst
    {
        get
        {
            for (int i = 0; i < Needs.Count; i++)
                if (Needs[i].Id == (int)BasicNeedId.Water) return Needs[i].Value;
            return 0;
        }
    }

    public string TraitLabel => Trait switch
    {
        NpcTrait.Leader => "Лидер",
        NpcTrait.Coward => "Трус",
        NpcTrait.Loner => "Одиночка",
        _ => ""
    };

    public string GenderLabel => Gender switch
    {
        Gender.Male => "М",
        Gender.Female => "Ж",
        _ => "?"
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

    public string EvolutionLabel => EvolutionLevel switch
    {
        0 => "Неробуджений",
        1 => "Пробудження",
        2 => "Усвідомлення",
        3 => "Злиття",
        4 => "Перетворення",
        5 => "Трансцендент",
        _ => EvolutionLevel.ToString()
    };

    public string StatusColor
    {
        get
        {
            if (!IsAlive) return "#3d1515";
            if (Health < 30) return "#3d2200";
            if (Hunger > 80 || Thirst > 80) return "#3d3200";
            return "#16213e";
        }
    }

    public void Remember(MemoryEntry entry)
    {
        Memory.Add(entry);
        if (Memory.Count > 50)
            Memory.RemoveAt(0);
    }

    public override string ToString() => Name;
}

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
