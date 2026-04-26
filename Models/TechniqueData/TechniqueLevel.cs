namespace ApocMinimal.Models.TechniqueData;

public enum TechniqueLevel
{
    Initiate,      // Новик          ×1.5
    Adept,         // Адепт          ×2
    Warrior,       // Воин           ×3
    Veteran,       // Ветеран        ×5
    Master,        // Мастер         ×8
    GrandMaster,   // Гранд-мастер   ×10
    Phantom,       // Призрак        ×12
    Legend,        // Легенда        ×15
    Vessel,        // Носитель       ×20
    Apex,          // Вершина        ×30
}

public enum TechniqueType
{
    Energy,   // Энергетическая — 60% на стат.23–30, 30% на 1–10,  10% на 11–22
    Physical, // Физическая     — 60% на стат.1–10,  30% на 23–30, 10% на 11–22
    Mental,   // Ментальная     — 60% на стат.11–22, 30% на 23–30, 10% на 1–10
}

public static class TechniqueLevelExtensions
{
    public static double GetMultiplier(this TechniqueLevel level) => level switch
    {
        TechniqueLevel.Initiate    => 1.5,
        TechniqueLevel.Adept       => 2.0,
        TechniqueLevel.Warrior     => 3.0,
        TechniqueLevel.Veteran     => 5.0,
        TechniqueLevel.Master      => 8.0,
        TechniqueLevel.GrandMaster => 10.0,
        TechniqueLevel.Phantom     => 12.0,
        TechniqueLevel.Legend      => 15.0,
        TechniqueLevel.Vessel      => 20.0,
        TechniqueLevel.Apex        => 30.0,
        _ => 1.0,
    };

    public static string ToLabel(this TechniqueLevel level) => level switch
    {
        TechniqueLevel.Initiate    => "Новик",
        TechniqueLevel.Adept       => "Адепт",
        TechniqueLevel.Warrior     => "Воин",
        TechniqueLevel.Veteran     => "Ветеран",
        TechniqueLevel.Master      => "Мастер",
        TechniqueLevel.GrandMaster => "Гранд-мастер",
        TechniqueLevel.Phantom     => "Призрак",
        TechniqueLevel.Legend      => "Легенда",
        TechniqueLevel.Vessel      => "Носитель",
        TechniqueLevel.Apex        => "Вершина",
        _ => level.ToString(),
    };

    /// <summary>Minimum altar level required to grant this technique.</summary>
    public static int MinAltarLevel(this TechniqueLevel level) => (int)level + 1;
}
