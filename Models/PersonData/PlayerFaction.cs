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
        factions.Add(new(PlayerFaction.ElementMages, "Архонты Стихий", "Мастера Ченджери. Энергетические характеристики последователей усилены.", "#79c0ff"));
        factions.Add(new(PlayerFaction.PathBlades, "Клинки Пути", "Мастера Ченджери. Энергетические характеристики последователей усилены.", "#f87171"));
        factions.Add(new(PlayerFaction.MirrorHealers, "Зеркальные Целители", "Мастера Ченджери. Энергетические характеристики последователей усилены.", "#56d364"));
        factions.Add(new(PlayerFaction.DeepSmiths, "Глубинные Кузнецы", "Мастера Ченджери. Энергетические характеристики последователей усилены.", "#e3b341"));
        factions.Add(new(PlayerFaction.GuardHeralds, "Страж-Вестники", "Мастера Ченджери. Энергетические характеристики последователей усилены.", "#d2a8ff"));
    }
}
