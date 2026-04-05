namespace ApocMinimal.Models;

public enum NpcTrait
{
    None,    // Обычный
    Leader,  // Лидер: +3 Faith соседям каждый день
    Coward,  // Трус: 50% шанс отказа от задания
    Loner    // Одиночка: иммунен к бонусу Лидера
}

public static class StatDefs
{
    // ── ФИЗИЧЕСКИЕ (1–10) ────────────────────────────────────
    public static readonly Dictionary<int, string> Names = new()
    {
        [1]  = "Сила",
        [2]  = "Ловкость",
        [3]  = "Выносливость",
        [4]  = "Сенсорика",
        [5]  = "Регенерация",
        [6]  = "Адаптация",
        [7]  = "Рефлексы",
        [8]  = "Восстановление сил",
        [9]  = "Стойкость",
        [10] = "Долголетие",

        // ── УМСТВЕННЫЕ (11–22) ───────────────────────────────
        [11] = "Пространств. интеллект",
        [12] = "Логика",
        [13] = "Память",
        [14] = "Соц.-эмоц. интеллект",
        [15] = "Когнитивная гибкость",
        [16] = "Фокус",
        [17] = "Интуиция",
        [18] = "Творчество",
        [19] = "Дедукция",
        [20] = "Математич. способности",
        [21] = "Воля",
        [22] = "Скорость обучения",

        // ── ЭНЕРГЕТИЧЕСКИЕ (23–30) ───────────────────────────
        [23] = "Запас энергии",
        [24] = "Восстановление энергии",
        [25] = "Концентрация энергии",
        [26] = "Контроль энергии",
        [27] = "Максимальный выход",
        [28] = "Тонкость манипуляций",
        [29] = "Энергетич. восприятие",
        [30] = "Энергетич. стойкость",
    };

    public static readonly Dictionary<string, int[]> Categories = new()
    {
        ["⚔ ФИЗИЧЕСКИЕ"]    = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
        ["🧠 УМСТВЕННЫЕ"]   = new[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22 },
        ["✦ ЭНЕРГЕТИЧЕСКИЕ"] = new[] { 23, 24, 25, 26, 27, 28, 29, 30 },
    };
}

public class Npc
{
    public int      Id              { get; set; }
    public string   Name            { get; set; } = "";
    public int      Age             { get; set; }
    public string   Profession      { get; set; } = "";
    public double   Health          { get; set; }
    public double   Faith           { get; set; }
    public double   Hunger          { get; set; }
    public double   Thirst          { get; set; }
    public NpcTrait Trait           { get; set; }
    public string   ActiveTask      { get; set; } = "";
    public int      TaskDaysLeft    { get; set; }
    public int      TaskRewardResId { get; set; }
    public double   TaskRewardAmt   { get; set; }

    /// <summary>30 характеристик: ключ = ID из StatDefs.Names, значение 0–100</summary>
    public Dictionary<int, double> Stats { get; set; } = new();

    public bool IsAlive => Health > 0;
    public bool HasTask => !string.IsNullOrEmpty(ActiveTask);

    public string TraitLabel => Trait switch
    {
        NpcTrait.Leader => "Лидер",
        NpcTrait.Coward => "Трус",
        NpcTrait.Loner  => "Одиночка",
        _               => ""
    };

    public string StatusColor
    {
        get
        {
            if (!IsAlive)                    return "#3d1515";
            if (Health < 30)                 return "#3d2200";
            if (Hunger > 80 || Thirst > 80)  return "#3d3200";
            return "#16213e";
        }
    }
}
