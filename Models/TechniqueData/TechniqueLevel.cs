namespace ApocMinimal.Models.TechniqueData;

public enum TechniqueLevel
{
    Genin,        // Генин        ×1.5
    EliteGenin,   // Элит-генин   ×2
    Chunin,       // Чонин        ×3
    EliteChunin,  // Элит-чонин   ×5
    Jonin,        // Джонин       ×8
    EliteJonin,   // Элит-джонин  ×10
    Anbu,         // АНБУ         ×12
    Sannin,       // Санин        ×15
    Jinchuriki,   // Джинчурики   ×20
    Kage,         // Каге         ×30
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
        TechniqueLevel.Genin => 1.5,
        TechniqueLevel.EliteGenin => 2.0,
        TechniqueLevel.Chunin => 3.0,
        TechniqueLevel.EliteChunin => 5.0,
        TechniqueLevel.Jonin => 8.0,
        TechniqueLevel.EliteJonin => 10.0,
        TechniqueLevel.Anbu => 12.0,
        TechniqueLevel.Sannin => 15.0,
        TechniqueLevel.Jinchuriki => 20.0,
        TechniqueLevel.Kage => 30.0,
        _ => 1.0,
    };

    public static string ToLabel(this TechniqueLevel level) => level switch
    {
        TechniqueLevel.Genin => "Генин",
        TechniqueLevel.EliteGenin => "Элит-генин",
        TechniqueLevel.Chunin => "Чонин",
        TechniqueLevel.EliteChunin => "Элит-чонин",
        TechniqueLevel.Jonin => "Джонин",
        TechniqueLevel.EliteJonin => "Элит-джонин",
        TechniqueLevel.Anbu => "АНБУ",
        TechniqueLevel.Sannin => "Санин",
        TechniqueLevel.Jinchuriki => "Джинчурики",
        TechniqueLevel.Kage => "Каге",
        _ => level.ToString(),
    };

    /// <summary>Minimum altar level required to grant this technique.</summary>
    public static int MinAltarLevel(this TechniqueLevel level) => (int)level + 1;
}
