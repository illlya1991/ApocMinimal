using ApocMinimal.Systems;
using System.Collections.Generic;
using System.Windows.Media;

namespace ApocMinimal.Models.PersonData;

/// <summary>Раса/клан Владельца ЦС — выбирается при старте игры.</summary>
public enum PlayerFaction
{
    ElementMages,   // Архонты Стихий — маги-стихийники
    PathBlades,     // Клинки Пути — универсальные мечники
    MirrorHealers,  // Зеркальные Целители — усилители тела
    DeepSmiths,     // Глубинные Кузнецы — гномы-ремесленники
    GuardHeralds,   // Страж-Вестники — воины-маги защиты
}

public class OnePlayerFaction
{
    public PlayerFaction Faction { get; set; }
    public string Label { get; set; }
    public string Description { get; set; }
    public string Color { get; set; }

    public OnePlayerFaction()
    {
        Faction = PlayerFaction.ElementMages;
        Label = "Архонты Стихий";
        Description = "Мастера Ченджери. Энергетические характеристики последователей усилены.";
        Color = "#79c0ff";
    }
    public OnePlayerFaction(PlayerFaction faction, string label, string description, string color)
    {
        Faction     = faction;
        Label       = label;
        Description = description;
        Color       = color;
    }
}
public class ListPlayerFactions
{
    public List<OnePlayerFaction> factions;

    public ListPlayerFactions()
    {
        factions = new List<OnePlayerFaction>();
        factions.Add(new(PlayerFaction.ElementMages, "Архонты Стихий",
            "Повелители четырёх стихий. Преобразуют энергию мира в ОР быстрее всех. " +
            "✦ +15% к генерации ОР. ✦ Последователи с высокой Преданностью дают двойной бонус. " +
            "✦ Уязвимость: физические угрозы наносят +20% урона.",
            "#79c0ff"));
        factions.Add(new(PlayerFaction.PathBlades, "Клинки Пути",
            "Мастера боевых искусств. Превращают победы над монстрами в источник силы. " +
            "✦ +25% к урону по фракциям монстров. ✦ Снижение угрозы при победе на 50% больше. " +
            "✦ Уязвимость: медленный рост Терминала (-1 к скорости прокачки).",
            "#f87171"));
        factions.Add(new(PlayerFaction.MirrorHealers, "Зеркальные Целители",
            "Целители разума и тела, отражающие чужую силу против врага. " +
            "✦ НПС восстанавливают здоровье вдвое быстрее. ✦ +10 к Преданности при вступлении в общину. " +
            "✦ Уязвимость: наступательные способности Терминала стоят +30% ОР.",
            "#56d364"));
        factions.Add(new(PlayerFaction.DeepSmiths, "Глубинные Кузнецы",
            "Мастера ресурсов и укреплений из-под земли. Строят быстрее и дешевле всех. " +
            "✦ +25% к добыче ресурсов. ✦ Защита барьера стоит −30% ОР. " +
            "✦ Уязвимость: НПС с низкой выносливостью работают медленнее.",
            "#e3b341"));
        factions.Add(new(PlayerFaction.GuardHeralds, "Страж-Вестники",
            "Хранители договоров и границ. Расширяют территорию быстрее и удерживают её дольше. " +
            "✦ Каждая защищённая локация даёт +2 ОР/день. ✦ Угроза монстров растёт на 20% медленнее. " +
            "✦ Уязвимость: боевые НПС приносят меньше ОР.",
            "#d2a8ff"));
    }
}
