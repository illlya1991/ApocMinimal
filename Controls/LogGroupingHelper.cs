using ApocMinimal.Models;

namespace ApocMinimal.Controls;

/// <summary>
/// Pure helper — all grouping math for the log time-hierarchy.
/// No WPF dependencies, no state.
/// </summary>
internal static class LogGroupingHelper
{
    internal static LogControl.TimeLevel GetChildLevel(LogControl.TimeLevel level) => level switch
    {
        LogControl.TimeLevel.Year    => LogControl.TimeLevel.Quarter,
        LogControl.TimeLevel.Quarter => LogControl.TimeLevel.Month,
        LogControl.TimeLevel.Month   => LogControl.TimeLevel.Week,
        LogControl.TimeLevel.Week    => LogControl.TimeLevel.Day,
        _                            => LogControl.TimeLevel.Day,
    };

    internal static string GetGroupKey(int day, LogControl.TimeLevel level) => level switch
    {
        LogControl.TimeLevel.Week    => $"W{GameCalendar.GetWeek(day):D3}_{GameCalendar.GetYear(day)}",
        LogControl.TimeLevel.Month   => $"M{GameCalendar.GetMonth(day):D2}_{GameCalendar.GetYear(day)}",
        LogControl.TimeLevel.Quarter => $"Q{GameCalendar.GetQuarter(day)}_{GameCalendar.GetYear(day)}",
        LogControl.TimeLevel.Year    => $"Y{GameCalendar.GetYear(day)}",
        _                            => $"D{day:D6}",
    };

    internal static int GetGroupSortKey(string key)
    {
        if (key.StartsWith("Y"))  return int.Parse(key[1..]) * 1_000_000;
        if (key.StartsWith("Q")) { var p = key[1..].Split('_'); return int.Parse(p[1]) * 1_000_000 + int.Parse(p[0]) * 100_000; }
        if (key.StartsWith("M")) { var p = key[1..].Split('_'); return int.Parse(p[1]) * 1_000_000 + int.Parse(p[0]) * 1_000; }
        if (key.StartsWith("W")) { var p = key[1..].Split('_'); return int.Parse(p[1]) * 1_000_000 + int.Parse(p[0]) * 10; }
        if (key.StartsWith("D")) return int.TryParse(key[1..], out int v) ? v : 0;
        return 0;
    }

    internal static string FormatGroupHeader(string key, int sampleDay, LogControl.TimeLevel level) => level switch
    {
        LogControl.TimeLevel.Week    => $"📅 Неделя {GameCalendar.GetWeek(sampleDay)}  ({GameCalendar.GetMonthName(sampleDay)} {GameCalendar.GetYear(sampleDay)})",
        LogControl.TimeLevel.Month   => $"📆 {GameCalendar.GetMonthName(sampleDay).ToUpperInvariant()}  {GameCalendar.GetYear(sampleDay)}",
        LogControl.TimeLevel.Quarter => $"🗓 Квартал {GameCalendar.GetQuarter(sampleDay)}  ({GameCalendar.GetYear(sampleDay)})",
        LogControl.TimeLevel.Year    => $"📂 {GameCalendar.GetYear(sampleDay)} год",
        _                            => ""
    };

    internal static string FormatSubGroupHeader(int sampleDay, LogControl.TimeLevel level) => level switch
    {
        LogControl.TimeLevel.Week    => $"📅 Неделя {GameCalendar.GetWeek(sampleDay)}",
        LogControl.TimeLevel.Month   => $"📆 {GameCalendar.GetMonthName(sampleDay)}",
        LogControl.TimeLevel.Quarter => $"🗓 Квартал {GameCalendar.GetQuarter(sampleDay)}",
        LogControl.TimeLevel.Year    => $"📂 {GameCalendar.GetYear(sampleDay)} год",
        _                            => ""
    };
}
