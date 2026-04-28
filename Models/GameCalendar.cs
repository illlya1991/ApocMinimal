using System;

namespace ApocMinimal.Models;

public static class GameCalendar
{
    public static readonly DateTime StartDate = new(2026, 3, 1); // 01.03.2026

    private static readonly string[] MonthNames =
    {
        "января", "февраля", "марта", "апреля", "мая", "июня",
        "июля", "августа", "сентября", "октября", "ноября", "декабря"
    };

    private static readonly string[] WeekdayNames =
    {
        "воскресенье", "понедельник", "вторник", "среда",
        "четверг", "пятница", "суббота"
    };

    private static readonly string[] WeekdayShortNames =
    {
        "Вс", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб"
    };

    private static readonly string[] SeasonNames =
    {
        "Зима", "Весна", "Лето", "Осень"
    };

    public static DateTime GetDate(int day) => StartDate.AddDays(day - 1);

    public static string GetDateString(int day)
    {
        var date = GetDate(day);
        return $"{date.Day} {MonthNames[date.Month - 1]} {date.Year}";
    }

    public static string GetWeekday(int day) => WeekdayNames[(int)GetDate(day).DayOfWeek];
    public static string GetWeekdayShort(int day) => WeekdayShortNames[(int)GetDate(day).DayOfWeek];

    public static int GetWeek(int day) => (day - 1) / 7 + 1;
    public static int GetMonth(int day) => GetDate(day).Month;
    public static string GetMonthName(int day) => MonthNames[GetDate(day).Month - 1];

    public static string GetSeason(int day)
    {
        int month = GetMonth(day);
        return month switch
        {
            12 or 1 or 2 => SeasonNames[0],
            3 or 4 or 5 => SeasonNames[1],
            6 or 7 or 8 => SeasonNames[2],
            _ => SeasonNames[3]
        };
    }

    public static int GetYear(int day) => GetDate(day).Year;

    public static int GetQuarter(int day) => (GetMonth(day) - 1) / 3 + 1;

    public static string GetQuarterName(int day) => $"Q{GetQuarter(day)}";

    public static (int Week, int Month, string Season, int Year) GetTimeHierarchy(int day) =>
        (GetWeek(day), GetMonth(day), GetSeason(day), GetYear(day));
}