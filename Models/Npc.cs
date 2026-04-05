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
    public static readonly Dictionary<int, string> Names = new()
    {
        [1]  = "Сила",
        [2]  = "Ловкость",
        [3]  = "Интеллект",
        [4]  = "Выносливость",
        [5]  = "Восприятие",
        [6]  = "Харизма",
        [7]  = "Воля",
        [8]  = "Удача",
        [9]  = "Скорость",
        [10] = "Меткость",
        [11] = "Скрытность",
        [12] = "Механика",
        [13] = "Медицина",
        [14] = "Выживание",
        [15] = "Торговля",
        [16] = "Лидерство",
        [17] = "Строительство",
        [18] = "Готовка",
        [19] = "Охота",
        [20] = "Земледелие",
        [21] = "Электроника",
        [22] = "Химия",
        [23] = "Боевые искусства",
        [24] = "Стрельба",
        [25] = "Ближний бой",
        [26] = "Защита",
        [27] = "Медитация",
        [28] = "Интуиция",
        [29] = "Память",
        [30] = "Адаптация",
    };
}

public class Npc
{
    public int      Id             { get; set; }
    public string   Name           { get; set; } = "";
    public int      Age            { get; set; }
    public string   Profession     { get; set; } = "";
    public double   Health         { get; set; }
    public double   Faith          { get; set; }
    public double   Hunger         { get; set; }
    public double   Thirst         { get; set; }
    public NpcTrait Trait          { get; set; }
    public string   ActiveTask     { get; set; } = "";
    public int      TaskDaysLeft   { get; set; }
    public int      TaskRewardResId{ get; set; }
    public double   TaskRewardAmt  { get; set; }

    // 30 характеристик: ключ = StatDefs.Names, значение 0–100
    public Dictionary<int, double> Stats { get; set; } = new();

    public bool IsAlive  => Health > 0;
    public bool HasTask  => !string.IsNullOrEmpty(ActiveTask);

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
            if (!IsAlive)                        return "#3d1515";
            if (Health < 30)                     return "#3d2200";
            if (Hunger > 80 || Thirst > 80)      return "#3d3200";
            return "#16213e";
        }
    }
}
