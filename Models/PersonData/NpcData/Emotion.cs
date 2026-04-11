namespace ApocMinimal.Models.PersonData.NpcData;

/// <summary>
/// One emotional state. An NPC has exactly 3 emotions whose Percentage values sum to 100.
/// </summary>
public class Emotion
{
    public string Name { get; set; } = "";
    public double Percentage { get; set; }   // 0–100, all 3 must sum to 100

    public Emotion() { }
    public Emotion(string name, double pct) { Name = name; Percentage = pct; }
}

public static class EmotionNames
{
    public static readonly string[] All =
    {
        "Радость",   "Грусть",      "Злость",    "Страх",
        "Спокойствие","Тревога",    "Надежда",   "Отчаяние",
        "Любовь",    "Ненависть",   "Скука",     "Воодушевление",
        "Смущение",  "Гордость",    "Вина",      "Стыд",
        "Зависть",   "Благодарность","Удивление", "Отвращение",
    };

    /// <summary>
    /// Picks 3 distinct emotions and distributes 100% randomly between them.
    /// </summary>
    public static List<Emotion> GenerateRandom(Random rnd)
    {
        var names = All.OrderBy(_ => rnd.Next()).Take(3).ToList();
        double a = rnd.Next(10, 60);
        double b = rnd.Next(10, (int)(100 - a - 10));
        double c = 100 - a - b;
        return new List<Emotion>
        {
            new(names[0], a),
            new(names[1], b),
            new(names[2], c),
        };
    }
}
