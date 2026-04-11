namespace ApocMinimal.Models.UIData;

public class LogEntry
{
    public string Text { get; set; } = "";
    public string Color { get; set; } = "#e0e0e0";

    // Цвета по типу события
    public static string ColorNormal => "#c8c8c8";
    public static string ColorSuccess => "#4ade80";  // зелёный
    public static string ColorWarning => "#fbbf24";  // жёлтый
    public static string ColorDanger => "#f87171";  // красный
    public static string ColorDay => "#60a5fa";  // синий  — системное
    public static string ColorSpeech => "#e879f9";  // фиолетовый — речь НПС
    public static string ColorAltarColor => "#facc15"; // золотой — алтарь
}
