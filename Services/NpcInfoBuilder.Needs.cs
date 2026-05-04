using System.Windows;
using System.Windows.Controls;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;

namespace ApocMinimal.Services;

public static partial class NpcInfoBuilder
{
    // ============================================================
    // Потребности
    // ============================================================

    private static readonly Dictionary<string, string[]> NeedLevelDescs = new()
    {
        ["Еда"]                  = new[]{"1 раз в 2 дня","1 раз в день","2 раза в день","3 раза в день","4+ раза в день"},
        ["Вода"]                 = new[]{"1 раз в 2 дня","1 раз в день","2 раза в день","3–4 раза в день","постоянно"},
        ["Сон"]                  = new[]{"4–5 ч/сутки","6 ч/сутки","7–8 ч/сутки","9 ч/сутки","10+ ч/сутки"},
        ["Тепло"]                = new[]{"переносит холод","небольшой обогрев","комфортная t°","тепло обязательно","постоянный комфорт"},
        ["Гигиена"]              = new[]{"раз в неделю","через день","ежедневно","2 раза в день","несколько раз в день"},
        ["Безопасность"]         = new[]{"почти не беспокоит","небольшая тревога","нужно чувствовать защиту","нужна охрана","постоянная охрана"},
        ["Отдых и здоровье"]     = new[]{"раз в неделю","2–3 раза в нед.","ежедневно","несколько раз в день","постоянный уход"},
        ["Общение"]              = new[]{"раз в неделю","2–3 раза в нед.","ежедневно","несколько раз в день","постоянное общение"},
        ["Секс/Семья"]           = new[]{"редко","иногда","регулярно","часто","постоянная близость"},
        ["Самосовершенствование"]= new[]{"редко учится","иногда","регулярно","активно","ежедневно"},
        ["Гурман"]               = new[]{"изредка деликатесы","иногда","регулярно","часто","только изысканное"},
        ["Сомелье"]              = new[]{"редко напитки","иногда","регулярно","часто","постоянно"},
        ["Чуткий сон"]           = new[]{"шум не мешает","немного мешает","нужна тишина","нужна тишь+кровать","идеальные условия"},
        ["Неженка"]              = new[]{"терпит дискомфорт","слабо ощущает","нужен уют","нужен комфорт","постоянный комфорт"},
        ["Эстет"]                = new[]{"редко ухаживает","иногда","регулярно","часто","ежедневный уход"},
        ["Параноик"]             = new[]{"слабая тревога","иногда проверяет","регулярно проверяет","постоянно проверяет","навязчивая тревога"},
        ["Гедонист"]             = new[]{"иногда удовольствие","регулярно","часто","очень часто","постоянно"},
        ["Светский лев"]         = new[]{"редкое общение","иногда","регулярно","часто","постоянно в обществе"},
        ["Романтик"]             = new[]{"романтика редко","иногда","регулярно","часто","постоянно"},
        ["Перфекционист"]        = new[]{"иногда совершенствует","регулярно","часто","очень часто","всегда лучшее"},
    };

    private static string GetLevelDesc(string needName, int level)
    {
        if (NeedLevelDescs.TryGetValue(needName, out var descs) && level >= 1 && level <= 5)
            return descs[level - 1];
        return $"уровень {level}";
    }

    private static string LevelStars(int level) =>
        new string('★', level) + new string('☆', 5 - level);

    private static string GetNeedColor(Need need)
    {
        if (need.IsCritical)  return "#f87171";
        if (need.IsUrgent)    return "#fbbf24";
        if (need.IsSatisfied) return "#4ade80";
        return "#c9d1d9";
    }

    public static void AddAllNeeds(StackPanel panel, Npc npc)
    {
        var basic   = npc.Needs.Where(n => n.Category == NeedCategory.Basic).OrderBy(n => n.Id).ToList();
        var special = npc.Needs.Where(n => n.Category == NeedCategory.Special).OrderBy(n => n.Id).ToList();

        panel.Children.Add(CreateSectionHeader("БАЗОВЫЕ ПОТРЕБНОСТИ"));
        foreach (var need in basic)
            AddNeedRow(panel, need);

        if (special.Count > 0)
        {
            panel.Children.Add(CreateSectionHeader("СПЕЦИАЛЬНЫЕ ПОТРЕБНОСТИ"));
            foreach (var need in special)
                AddNeedRow(panel, need);
        }
    }

    private static void AddNeedRow(StackPanel panel, Need need)
    {
        string color = GetNeedColor(need);
        string stars = LevelStars(need.Level);
        string desc  = GetLevelDesc(need.Name, need.Level);
        string state = need.IsCritical ? "КРИТИЧНО" : need.IsUrgent ? "срочно" : need.IsSatisfied ? "ok" : $"{need.Value:F0}%";

        var grid = new Grid { Margin = new Thickness(0, 1, 0, 1) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150, GridUnitType.Pixel) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80,  GridUnitType.Pixel) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50,  GridUnitType.Pixel) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1,   GridUnitType.Star)  });

        grid.Children.Add(new TextBlock { Text = need.Name, Foreground = GetBrush(color), FontSize = 12 });
        var starsBlock = new TextBlock { Text = stars, Foreground = GetBrush("#facc15"), FontSize = 10 };
        Grid.SetColumn(starsBlock, 1);
        grid.Children.Add(starsBlock);
        var stateBlock = new TextBlock { Text = state, Foreground = GetBrush(color), FontSize = 11, FontWeight = need.IsCritical ? FontWeights.Bold : FontWeights.Normal };
        Grid.SetColumn(stateBlock, 2);
        grid.Children.Add(stateBlock);
        var descBlock = new TextBlock { Text = desc, Foreground = GetBrush("#8b949e"), FontSize = 11 };
        Grid.SetColumn(descBlock, 3);
        grid.Children.Add(descBlock);

        panel.Children.Add(grid);
    }
}
