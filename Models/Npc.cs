namespace ApocMinimal.Models;

public enum NpcTrait
{
    None,    // Обычный
    Leader,  // Лидер: +3 Faith соседям каждый день
    Coward,  // Трус: 50% шанс отказа от задания
    Loner    // Одиночка: иммунен к бонусу Лидера, тихий
}

public class Npc
{
    public int      Id             { get; set; }
    public string   Name           { get; set; } = "";
    public int      Age            { get; set; }
    public string   Profession     { get; set; } = "";
    public double   Health         { get; set; }
    public double   Faith          { get; set; }
    public double   Hunger         { get; set; }   // 0–100, растёт каждый день
    public double   Thirst         { get; set; }   // 0–100, растёт каждый день
    public NpcTrait Trait          { get; set; }
    public string   ActiveTask     { get; set; } = "";
    public int      TaskDaysLeft   { get; set; }
    public int      TaskRewardResId{ get; set; }
    public double   TaskRewardAmt  { get; set; }

    public bool IsAlive  => Health > 0;
    public bool HasTask  => !string.IsNullOrEmpty(ActiveTask);

    public string TraitLabel => Trait switch
    {
        NpcTrait.Leader => "Лидер",
        NpcTrait.Coward => "Трус",
        NpcTrait.Loner  => "Одиночка",
        _               => ""
    };

    // Цвет карточки в зависимости от состояния
    public string StatusColor
    {
        get
        {
            if (!IsAlive)        return "#3d1515";
            if (Health < 30)     return "#3d2200";
            if (Hunger > 80 || Thirst > 80) return "#3d3200";
            return "#16213e";
        }
    }
}
