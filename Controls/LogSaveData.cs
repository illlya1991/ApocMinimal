namespace ApocMinimal.Controls;

public class LogEntryData
{
    public string Section  { get; set; } = "";
    public string Text     { get; set; } = "";
    public string Color    { get; set; } = "#c9d1d9";
    public bool   IsAction { get; set; }
}

public class LogDayData
{
    public int              DayNumber { get; set; }
    public List<LogEntryData> Entries { get; set; } = new();
}
