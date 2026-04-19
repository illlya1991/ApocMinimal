// NpcInfoBuilder.cs - исправленная версия

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.StatisticsData;
using ApocMinimal.Models.TechniqueData;

namespace ApocMinimal.Services;

public static class NpcInfoBuilder
{
    // ============================================================
    // Цвета
    // ============================================================

    public static string GetStatColor(int value)
    {
        if (value >= 75) return "#4ade80";
        if (value >= 50) return "#c9d1d9";
        return "#fbbf24";
    }

    public static SolidColorBrush GetStatColorBrush(int value)
    {
        string hex = GetStatColor(value);
        return (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
    }

    private static SolidColorBrush GetBrush(string hex)
    {
        return (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
    }

    // ============================================================
    // Базовые UI элементы
    // ============================================================

    public static TextBlock CreateSectionHeader(string title, bool isLarge = false)
    {
        return new TextBlock
        {
            Text = title,
            Foreground = GetBrush("#60a5fa"),
            FontSize = isLarge ? 13 : 11,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, isLarge ? 15 : 10, 0, isLarge ? 8 : 5)
        };
    }

    public static Grid CreateInfoRow(string label, string value, string colorHex, double leftMargin = 0, string? bgHex = null)
    {
        Grid grid = new Grid();
        if (bgHex != null)
            grid.Background = GetBrush(bgHex);
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140, GridUnitType.Pixel) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        TextBlock labelBlock = new TextBlock
        {
            Text = label,
            Foreground = GetBrush("#8b949e"),
            FontSize = 12,
            Margin = new Thickness(leftMargin, 2, 0, 2)
        };

        TextBlock valueBlock = new TextBlock
        {
            Text = value,
            Foreground = GetBrush(colorHex),
            FontSize = 12,
            Margin = new Thickness(5, 2, 0, 2)
        };

        grid.Children.Add(labelBlock);
        grid.Children.Add(valueBlock);
        Grid.SetColumn(valueBlock, 1);
        return grid;
    }

    // ============================================================
    // Сравнение (compare helpers)
    // ============================================================

    private static bool NumSame(double a, double b) => Math.Abs(a - b) <= 5;
    private static bool TextSame(string a, string b) => string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string? CmpBg(bool areSame, bool hlSame, bool hlDiff)
    {
        if (areSame && hlSame) return "#0d2a0d";
        if (!areSame && hlDiff) return "#2a0d0d";
        return null;
    }

    private static string NumValue(double val, double other) =>
        val == other ? val.ToString("F0")
        : val > other ? $"{val:F0} (▲{val - other:+0;+0})"
        : $"{val:F0} (▼{val - other:+0;+0})";

    public static StackPanel BuildCompareHighlightLegend(bool hlSame, bool hlDiff)
    {
        StackPanel p = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        if (hlSame)
        {
            Border b = new Border { Background = GetBrush("#0d2a0d"), Width = 12, Height = 12, CornerRadius = new CornerRadius(2), Margin = new Thickness(0, 0, 4, 0) };
            p.Children.Add(b);
            p.Children.Add(new TextBlock { Text = "одинаковые", Foreground = GetBrush("#4ade80"), FontSize = 11, Margin = new Thickness(0, 0, 12, 0) });
        }
        if (hlDiff)
        {
            Border b = new Border { Background = GetBrush("#2a0d0d"), Width = 12, Height = 12, CornerRadius = new CornerRadius(2), Margin = new Thickness(0, 0, 4, 0) };
            p.Children.Add(b);
            p.Children.Add(new TextBlock { Text = "различаются", Foreground = GetBrush("#f87171"), FontSize = 11 });
        }
        return p;
    }

    public static void AddStatsByTypeCompare(StackPanel panel, Npc npc, Npc other, StatType type, bool hlSame, bool hlDiff)
    {
        List<Characteristic> stats = npc.Stats.GetStatsByType(type);
        List<Characteristic> otherStats = other.Stats.GetStatsByType(type);
        for (int i = 0; i < stats.Count; i++)
        {
            Characteristic stat = stats[i];
            double otherVal = i < otherStats.Count ? otherStats[i].FinalValue : 0;
            string? bg = CmpBg(NumSame(stat.FinalValue, otherVal), hlSame, hlDiff);
            string display = NumValue(stat.FinalValue, otherVal);
            panel.Children.Add(CreateInfoRow(stat.Name + ":", display, GetStatColor(stat.FinalValue), 0, bg));
        }
    }

    public static StackPanel BuildFullInfoPanelWithCompare(Npc npc, Npc other, bool hlSame, bool hlDiff)
    {
        StackPanel panel = new StackPanel();

        panel.Children.Add(new TextBlock
        {
            Text = $"{npc.Name} [{npc.GenderLabel}] {npc.Age} лет",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = GetBrush("#60a5fa"),
            Margin = new Thickness(0, 0, 0, 4)
        });

        if (hlSame || hlDiff)
            panel.Children.Add(BuildCompareHighlightLegend(hlSame, hlDiff));

        string? N(double a, double b) => CmpBg(NumSame(a, b), hlSame, hlDiff);
        string? T(string a, string b) => CmpBg(TextSame(a, b), hlSame, hlDiff);

        panel.Children.Add(CreateSectionHeader("ОСНОВНЫЕ ХАРАКТЕРИСТИКИ"));
        panel.Children.Add(CreateInfoRow("HP:", NumValue(npc.Health, other.Health), npc.Health < 30 ? "#f87171" : "#4ade80", 0, N(npc.Health, other.Health)));
        panel.Children.Add(CreateInfoRow("Выносливость:", NumValue(npc.Stamina, other.Stamina), npc.Stamina < 30 ? "#f87171" : "#60a5fa", 0, N(npc.Stamina, other.Stamina)));
        panel.Children.Add(CreateInfoRow("Чакра:", NumValue(npc.Chakra, other.Chakra), "#e879f9", 0, N(npc.Chakra, other.Chakra)));
        panel.Children.Add(CreateInfoRow("Вера:", NumValue(npc.Faith, other.Faith), "#facc15", 0, N(npc.Faith, other.Faith)));
        panel.Children.Add(CreateInfoRow("Страх:", NumValue(npc.Fear, other.Fear), npc.Fear > 70 ? "#f87171" : "#c9d1d9", 0, N(npc.Fear, other.Fear)));
        panel.Children.Add(CreateInfoRow("Доверие:", NumValue(npc.Trust, other.Trust), npc.Trust > 70 ? "#4ade80" : "#c9d1d9", 0, N(npc.Trust, other.Trust)));
        panel.Children.Add(CreateInfoRow("Инициатива:", NumValue(npc.Initiative, other.Initiative), "#c9d1d9", 0, N(npc.Initiative, other.Initiative)));
        panel.Children.Add(CreateInfoRow("Уровень:", npc.FollowerLabel, "#d29922", 0, T(npc.FollowerLabel, other.FollowerLabel)));

        panel.Children.Add(CreateSectionHeader("ЛИЧНОСТЬ"));
        string traitsA = BuildTraitsString(npc.CharTraits);
        string traitsB = BuildTraitsString(other.CharTraits);
        panel.Children.Add(CreateInfoRow("Черты:", traitsA, "#d29922", 0, T(traitsA, traitsB)));
        string emotA = BuildEmotionsString(npc.Emotions);
        string emotB = BuildEmotionsString(other.Emotions);
        panel.Children.Add(CreateInfoRow("Эмоции:", emotA, "#e879f9", 0, T(emotA, emotB)));

        panel.Children.Add(CreateSectionHeader("ЦЕЛИ"));
        panel.Children.Add(CreateInfoRow("Цель:", npc.Goal, "#c9d1d9", 0, T(npc.Goal, other.Goal)));
        panel.Children.Add(CreateInfoRow("Мечта:", npc.Dream, "#c9d1d9", 0, T(npc.Dream, other.Dream)));
        panel.Children.Add(CreateInfoRow("Желание:", npc.Desire, "#c9d1d9", 0, T(npc.Desire, other.Desire)));

        if (npc.Specializations.Count > 0 || other.Specializations.Count > 0)
        {
            string specA = BuildSpecializationsString(npc.Specializations);
            string specB = BuildSpecializationsString(other.Specializations);
            panel.Children.Add(CreateInfoRow("Специализации:", specA, "#56d364", 0, T(specA, specB)));
        }

        panel.Children.Add(CreateSectionHeader("ПОТРЕБНОСТИ (срочные)"));
        bool hasUrgent = false;
        for (int i = 0; i < npc.Needs.Count; i++)
        {
            Need need = npc.Needs[i];
            if (need.IsUrgent || need.IsCritical)
            {
                hasUrgent = true;
                Need? otherNeed = null;
                for (int j = 0; j < other.Needs.Count; j++)
                    if (other.Needs[j].Name == need.Name) { otherNeed = other.Needs[j]; break; }
                string? bg = otherNeed != null ? N(need.Value, otherNeed.Value) : (hlDiff ? "#2a0d0d" : null);
                string display = otherNeed != null ? NumValue(need.Value, otherNeed.Value) : $"{need.Value:F0}%";
                panel.Children.Add(CreateInfoRow($"  {need.Name}:", display, need.IsCritical ? "#f87171" : "#fbbf24", 0, bg));
            }
        }
        if (!hasUrgent)
            panel.Children.Add(new TextBlock { Text = "  Нет срочных потребностей", Foreground = GetBrush("#4ade80"), FontSize = 11, Margin = new Thickness(0, 2, 0, 2) });

        Expander statsExpander = CreateCollapsibleSection("ХАРАКТЕРИСТИКИ", true);
        StackPanel statsPanel = new StackPanel { Margin = new Thickness(15, 0, 0, 0) };
        statsPanel.Children.Add(CreateSectionHeader("ФИЗИЧЕСКИЕ"));
        AddStatsByTypeCompare(statsPanel, npc, other, StatType.Physical, hlSame, hlDiff);
        statsPanel.Children.Add(CreateSectionHeader("МЕНТАЛЬНЫЕ"));
        AddStatsByTypeCompare(statsPanel, npc, other, StatType.Mental, hlSame, hlDiff);
        statsPanel.Children.Add(CreateSectionHeader("ЭНЕРГЕТИЧЕСКИЕ"));
        AddStatsByTypeCompare(statsPanel, npc, other, StatType.Energy, hlSame, hlDiff);
        statsExpander.Content = statsPanel;
        panel.Children.Add(statsExpander);

        return panel;
    }

    public static Expander CreateCollapsibleSection(string header, bool IsExpanded = false)
    {
        Expander expander = new Expander();
        expander.Header = header;
        expander.IsExpanded = IsExpanded;
        expander.Foreground = GetBrush("#60a5fa");
        expander.FontSize = 12;
        expander.FontWeight = FontWeights.SemiBold;
        expander.Margin = new Thickness(0, 10, 0, 5);
        return expander;
    }

    // ============================================================
    // Статистические панели
    // ============================================================

    public static void AddStatsByType(StackPanel panel, Npc npc, StatType type, bool showIcons = false)
    {
        List<Characteristic> stats = npc.Stats.GetStatsByType(type);
        for (int i = 0; i < stats.Count; i++)
        {
            Characteristic stat = stats[i];
            string icon = "";
            if (showIcons)
            {
                if (stat.IsCombat) icon = "⚔ ";
                if (stat.IsSocial) icon = "💬 ";
            }
            panel.Children.Add(CreateInfoRow(icon + stat.Name + ":", stat.FinalValue.ToString(), GetStatColor(stat.FinalValue)));
        }
    }

    /// <summary>
    /// Добавляет только боевые характеристики (IsCombat = true)
    /// </summary>
    public static void AddCombatStats(StackPanel panel, Npc npc)
    {
        List<Characteristic> stats = npc.Stats.GetCombatStats();
        for (int i = 0; i < stats.Count; i++)
        {
            Characteristic stat = stats[i];
            panel.Children.Add(CreateInfoRow(stat.Name + ":", stat.FinalValue.ToString(), GetStatColor(stat.FinalValue)));
        }
        panel.Children.Add(CreateInfoRow("Боевая инициатива:", npc.CombatInitiative.ToString("F0"), "#c9d1d9"));
    }

    /// <summary>
    /// Добавляет только социальные характеристики (IsSocial = true)
    /// </summary>
    public static void AddSocialStats(StackPanel panel, Npc npc)
    {
        List<Characteristic> stats = npc.Stats.GetSocialStats();
        for (int i = 0; i < stats.Count; i++)
        {
            Characteristic stat = stats[i];
            panel.Children.Add(CreateInfoRow(stat.Name + ":", stat.FinalValue.ToString(), GetStatColor(stat.FinalValue)));
        }
    }

    public static void AddAllStats(StackPanel panel, Npc npc, bool showIcons = false)
    {
        panel.Children.Add(CreateSectionHeader("ФИЗИЧЕСКИЕ"));
        AddStatsByType(panel, npc, StatType.Physical, showIcons);
        panel.Children.Add(CreateSectionHeader("МЕНТАЛЬНЫЕ"));
        AddStatsByType(panel, npc, StatType.Mental, showIcons);
        panel.Children.Add(CreateSectionHeader("ЭНЕРГЕТИЧЕСКИЕ"));
        AddStatsByType(panel, npc, StatType.Energy, showIcons);
    }

    // ============================================================
    // Детальные статы с модификаторами
    // ============================================================

    public static void AddDetailedStats(StackPanel panel, Npc npc)
    {
        // Физические
        panel.Children.Add(CreateSectionHeader("ФИЗИЧЕСКИЕ", true));
        AddDetailedStatGroup(panel, npc.Stats.GetStatsByType(StatType.Physical));

        // Ментальные
        panel.Children.Add(CreateSectionHeader("МЕНТАЛЬНЫЕ", true));
        AddDetailedStatGroup(panel, npc.Stats.GetStatsByType(StatType.Mental));

        // Энергетические
        panel.Children.Add(CreateSectionHeader("ЭНЕРГЕТИЧЕСКИЕ", true));
        AddDetailedStatGroup(panel, npc.Stats.GetStatsByType(StatType.Energy));
    }

    private static void AddDetailedStatGroup(StackPanel panel, List<Characteristic> stats)
    {
        for (int i = 0; i < stats.Count; i++)
        {
            Characteristic stat = stats[i];
            string combatIcon = stat.IsCombat ? "⚔ " : "  ";
            string socialIcon = stat.IsSocial ? "💬 " : "  ";

            panel.Children.Add(new TextBlock
            {
                Text = "  " + combatIcon + socialIcon + stat.Name + ":",
                Foreground = GetBrush("#8b949e"),
                FontSize = 11,
                Margin = new Thickness(0, 4, 0, 2)
            });

            string deviationText;
            if (stat.Deviation > 0)
                deviationText = "+" + stat.Deviation.ToString();
            else if (stat.Deviation < 0)
                deviationText = stat.Deviation.ToString();
            else
                deviationText = " 0";

            panel.Children.Add(new TextBlock
            {
                Text = "      База: " + stat.BaseValue.ToString().PadLeft(3) +
                       " | Откл.: " + deviationText.PadLeft(3) +
                       " | Полн.база: " + stat.FullBase.ToString().PadLeft(3) +
                       " | Итог: " + stat.FinalValue.ToString().PadLeft(3),
                Foreground = GetStatColorBrush(stat.FinalValue),
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 2)
            });

            // Постоянные модификаторы
            List<PermanentModifier> permMods = stat.GetModifiersByType<PermanentModifier>();
            for (int j = 0; j < permMods.Count; j++)
            {
                PermanentModifier mod = permMods[j];
                if (mod.IsActive())
                {
                    string modSign = (mod.Type == ModifierType.Additive) ? "+" : "×";
                    panel.Children.Add(new TextBlock
                    {
                        Text = "        [П] " + mod.Name + ": " + modSign + mod.Value.ToString() + " (" + mod.Source + ")",
                        Foreground = GetBrush("#facc15"),
                        FontSize = 10,
                        Margin = new Thickness(0, 0, 0, 1)
                    });
                }
            }

            // Временные модификаторы
            List<IndependentModifier> indMods = stat.GetModifiersByType<IndependentModifier>();
            for (int j = 0; j < indMods.Count; j++)
            {
                IndependentModifier mod = indMods[j];
                if (mod.IsActive())
                {
                    string modSign = (mod.Type == ModifierType.Additive) ? "+" : "×";
                    string timeLeft = "";
                    if (mod.TimeUnit == TimeUnit.Days)
                    {
                        timeLeft = " (ост. " + mod.Remaining.ToString() + " дн.)";
                    }
                    else if (mod.TimeUnit == TimeUnit.Hours)
                    {
                        timeLeft = " (ост. " + mod.Remaining.ToString() + " ч.)";
                    }
                    else if (mod.TimeUnit == TimeUnit.CombatTurns)
                    {
                        timeLeft = " (ост. " + mod.Remaining.ToString() + " ходов)";
                    }
                    panel.Children.Add(new TextBlock
                    {
                        Text = "        [В] " + mod.Name + ": " + modSign + mod.Value.ToString() + timeLeft + " (" + mod.Source + ")",
                        Foreground = GetBrush("#fbbf24"),
                        FontSize = 10,
                        Margin = new Thickness(0, 0, 0, 1)
                    });
                }
            }
        }
    }

    // ============================================================
    // Строковые представления
    // ============================================================

    public static string BuildTraitsString(List<CharacterTrait> traits)
    {
        if (traits.Count == 0) return "Нет";
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < traits.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(traits[i].ToLabel());
        }
        return sb.ToString();
    }

    public static string BuildEmotionsString(List<Emotion> emotions)
    {
        if (emotions.Count == 0) return "Нет";
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < emotions.Count; i++)
        {
            if (i > 0) sb.Append(" | ");
            sb.Append(emotions[i].Name);
            sb.Append(' ');
            sb.Append(emotions[i].Percentage.ToString("F0"));
            sb.Append('%');
        }
        return sb.ToString();
    }

    public static string BuildSpecializationsString(List<string> specializations)
    {
        if (specializations.Count == 0) return "Нет";
        return string.Join(", ", specializations);
    }

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
        ["Самосовершенствование"]= new[]{"редко учится","иногда","регулярно","активно","ежедневно" },
        ["Гурман"]               = new[]{"изредка деликатесы","иногда","регулярно","часто","только изысканное"},
        ["Сомелье"]              = new[]{"редко напитки","иногда","регулярно","часто","постоянно"},
        ["Чуткий сон"]           = new[]{"шум не мешает","немного мешает","нужна тишина","нужна тишь+кровать","идеальные условия"},
        ["Неженка"]              = new[]{"терпит дискомфорт","слабо ощущает","нужен уют","нужен комфорт","постоянный комфорт"},
        ["Эстет"]                = new[]{"редко ухаживает","иногда","регулярно","часто","ежедневный уход"},
        ["Параноик"]             = new[]{"слабая тревога","иногда проверяет","регулярно проверяет","постоянно проверяет","навязчивая тревога"},
        ["Гедонист"]             = new[]{"иногда удовольствие","регулярно","часто","очень часто","постоянно"},
        ["Светский лев"]         = new[]{"редкое общение","иногда","регулярно","часто","постоянно в обществе"},
        ["Романтик"]             = new[]{"романтика редко","иногда","регулярно","часто","постоянно"},
        ["Перфекционист"]        = new[]{"иногда совершенствует","регулярно","часто","очень часто","всегда лучшее" },
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
        if (need.IsCritical) return "#f87171";
        if (need.IsUrgent)   return "#fbbf24";
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

    // ============================================================
    // Полная информационная панель
    // ============================================================

    public static StackPanel BuildFullInfoPanel(Npc npc, bool isCompact = false)
    {
        StackPanel panel = new StackPanel();

        // Заголовок
        panel.Children.Add(new TextBlock
        {
            Text = $"{npc.Name} [{npc.GenderLabel}] {npc.Age} лет",
            FontSize = isCompact ? 14 : 16,
            FontWeight = FontWeights.Bold,
            Foreground = GetBrush("#60a5fa"),
            Margin = new Thickness(0, 0, 0, isCompact ? 8 : 10)
        });

        // Основные характеристики
        panel.Children.Add(CreateSectionHeader("ОСНОВНЫЕ ХАРАКТЕРИСТИКИ"));
        panel.Children.Add(CreateInfoRow("HP:", $"{npc.Health:F0}", npc.Health < 30 ? "#f87171" : "#4ade80"));
        panel.Children.Add(CreateInfoRow("Выносливость:", $"{npc.Stamina:F0}", npc.Stamina < 30 ? "#f87171" : "#60a5fa"));
        panel.Children.Add(CreateInfoRow("Чакра:", $"{npc.Chakra:F0}", "#e879f9"));
        panel.Children.Add(CreateInfoRow("Вера:", $"{npc.Faith:F0}", "#facc15"));
        panel.Children.Add(CreateInfoRow("Страх:", $"{npc.Fear:F0}", npc.Fear > 70 ? "#f87171" : "#c9d1d9"));
        panel.Children.Add(CreateInfoRow("Доверие:", $"{npc.Trust:F0}", npc.Trust > 70 ? "#4ade80" : "#c9d1d9"));
        panel.Children.Add(CreateInfoRow("Инициатива:", $"{npc.Initiative:F0}", "#c9d1d9"));
        panel.Children.Add(CreateInfoRow("Уровень:", npc.FollowerLabel, "#d29922"));

        // Личность
        panel.Children.Add(CreateSectionHeader("ЛИЧНОСТЬ"));
        panel.Children.Add(CreateInfoRow("Черты:", BuildTraitsString(npc.CharTraits), "#d29922"));
        panel.Children.Add(CreateInfoRow("Эмоции:", BuildEmotionsString(npc.Emotions), "#e879f9"));

        // Цели
        panel.Children.Add(CreateSectionHeader("ЦЕЛИ"));
        panel.Children.Add(CreateInfoRow("Цель:", npc.Goal, "#c9d1d9"));
        panel.Children.Add(CreateInfoRow("Мечта:", npc.Dream, "#c9d1d9"));
        panel.Children.Add(CreateInfoRow("Желание:", npc.Desire, "#c9d1d9"));

        // Специализации
        if (npc.Specializations.Count > 0)
        {
            panel.Children.Add(CreateInfoRow("Специализации:", BuildSpecializationsString(npc.Specializations), "#56d364"));
        }

        // Техники и способности
        AddTechAbilitySection(panel, npc);

        // Потребности
        if (isCompact)
        {
            // Detailed mode: все потребности с уровнем и описанием
            AddAllNeeds(panel, npc);
        }
        else
        {
            // Full mode: только срочные
            panel.Children.Add(CreateSectionHeader("ПОТРЕБНОСТИ (срочные)"));
            bool hasUrgent = false;
            for (int i = 0; i < npc.Needs.Count; i++)
            {
                Need need = npc.Needs[i];
                if (need.IsUrgent || need.IsCritical)
                {
                    hasUrgent = true;
                    panel.Children.Add(CreateInfoRow($"  {need.Name}:", $"{need.Value:F0}%", need.IsCritical ? "#f87171" : "#fbbf24"));
                }
            }
            if (!hasUrgent)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "  Нет срочных потребностей",
                    Foreground = GetBrush("#4ade80"),
                    FontSize = 11,
                    Margin = new Thickness(0, 2, 0, 2)
                });
            }
        }

        // Характеристики (сворачиваемые)
        Expander statsExpander = CreateCollapsibleSection("ХАРАКТЕРИСТИКИ", isCompact);
        StackPanel statsPanel = new StackPanel { Margin = new Thickness(15, 0, 0, 0) };
        if (!isCompact)
            AddAllStats(statsPanel, npc);
        else
            AddDetailedStats(statsPanel, npc);
        statsExpander.Content = statsPanel;
        panel.Children.Add(statsExpander);

        return panel;
    }

    // ============================================================
    // Техники и способности НПС
    // ============================================================

    public static void AddTechAbilitySection(StackPanel panel, Npc npc)
    {
        bool hasAbilities  = npc.LearnedAbilityIds.Count > 0;
        bool hasTechniques = npc.LearnedTechIds.Count  > 0;
        if (!hasAbilities && !hasTechniques) return;

        panel.Children.Add(CreateSectionHeader("ТЕХНИКИ И СПОСОБНОСТИ"));

        // Abilities first
        foreach (var abilId in npc.LearnedAbilityIds)
        {
            var abil = TechAbilityCatalog.FindAbility(abilId);
            string abilName = abil?.Name ?? abilId;

            panel.Children.Add(new TextBlock
            {
                Text = $"◆ {abilName}",
                Foreground = GetBrush("#c084fc"),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 3, 0, 1),
            });

            if (abil != null)
            {
                foreach (var techId in abil.TechniqueIds)
                {
                    var tech = TechAbilityCatalog.FindTech(techId);
                    if (tech == null) continue;
                    string kindLabel = tech.Kind == TechKind.Passive ? "Пассив" : "Актив";
                    string fgColor   = tech.Kind == TechKind.Passive ? "#7ee787" : "#79c0ff";
                    panel.Children.Add(new TextBlock
                    {
                        Text = $"   • {tech.Name} [{kindLabel}]",
                        Foreground = GetBrush(fgColor),
                        FontSize = 11,
                        Margin = new Thickness(0, 1, 0, 1),
                        ToolTip = tech.Kind == TechKind.Passive ? tech.Description
                            : $"{tech.Description}\n⚔ {tech.CombatEffect}\n🌿 {tech.LifeEffect}",
                    });
                }
            }
        }

        // Standalone techniques
        foreach (var techId in npc.LearnedTechIds)
        {
            var tech = TechAbilityCatalog.FindTech(techId);
            string techName  = tech?.Name ?? techId;
            string kindLabel = tech?.Kind == TechKind.Passive ? "Пассив" : "Актив";
            string fgColor   = tech?.Kind == TechKind.Passive ? "#7ee787" : "#79c0ff";
            panel.Children.Add(new TextBlock
            {
                Text = $"▸ {techName} [{kindLabel}]",
                Foreground = GetBrush(fgColor),
                FontSize = 12,
                Margin = new Thickness(0, 2, 0, 2),
                ToolTip = tech == null ? null : (tech.Kind == TechKind.Passive ? tech.Description
                    : $"{tech.Description}\n⚔ {tech.CombatEffect}\n🌿 {tech.LifeEffect}"),
            });
        }
    }

    public static StackPanel BuildCompactInfoPanel(Npc npc)
    {
        StackPanel panel = new StackPanel();

        panel.Children.Add(new TextBlock
        {
            Text = $"{npc.Name} [{npc.GenderLabel}] {npc.Age} лет",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = GetBrush("#60a5fa"),
            Margin = new Thickness(0, 0, 0, 8)
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"❤{npc.Health:F0} ✦{npc.Faith:F0} 😨{npc.Fear:F0} 🤝{npc.Trust:F0} 💪{npc.Stats.Strength} 🧠{npc.Stats.Intelligence}",
            Foreground = GetBrush("#c9d1d9"),
            FontSize = 12,
            Margin = new Thickness(0, 5, 0, 5)
        });

        // Критические потребности
        bool hasCritical = false;
        StringBuilder criticalBuilder = new StringBuilder();
        for (int i = 0; i < npc.Needs.Count; i++)
        {
            Need need = npc.Needs[i];
            if (need.IsCritical)
            {
                if (hasCritical) criticalBuilder.Append(", ");
                criticalBuilder.Append(need.Name);
                hasCritical = true;
            }
        }

        if (hasCritical)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "⚠ КРИТИЧНО: " + criticalBuilder.ToString(),
                Foreground = GetBrush("#f87171"),
                FontSize = 11
            });
        }

        return panel;
    }

    public static StackPanel BuildCombatInfoPanel(Npc npc)
    {
        StackPanel panel = new StackPanel();

        panel.Children.Add(new TextBlock
        {
            Text = $"{npc.Name} [{npc.GenderLabel}]",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = GetBrush("#60a5fa"),
            Margin = new Thickness(0, 0, 0, 10)
        });

        panel.Children.Add(CreateSectionHeader("БОЕВЫЕ ХАРАКТЕРИСТИКИ"));
        AddCombatStats(panel, npc);
        return panel;
    }

    public static StackPanel BuildSocialInfoPanel(Npc npc)
    {
        StackPanel panel = new StackPanel();

        panel.Children.Add(new TextBlock
        {
            Text = $"{npc.Name} [{npc.GenderLabel}]",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = GetBrush("#60a5fa"),
            Margin = new Thickness(0, 0, 0, 10)
        });

        panel.Children.Add(CreateSectionHeader("СОЦИАЛЬНЫЕ ХАРАКТЕРИСТИКИ"));
        panel.Children.Add(CreateInfoRow("Доверие:", $"{npc.Trust:F0}", npc.Trust > 70 ? "#4ade80" : "#c9d1d9"));
        panel.Children.Add(CreateInfoRow("Вера:", $"{npc.Faith:F0}", "#facc15"));
        panel.Children.Add(CreateInfoRow("Уровень последователя:", npc.FollowerLabel, "#d29922"));
        panel.Children.Add(CreateInfoRow("Черты:", BuildTraitsString(npc.CharTraits), "#d29922"));
        panel.Children.Add(CreateInfoRow("Эмоции:", BuildEmotionsString(npc.Emotions), "#e879f9"));
        panel.Children.Add(CreateInfoRow("Цель:", npc.Goal, "#c9d1d9"));
        panel.Children.Add(CreateInfoRow("Мечта:", npc.Dream, "#c9d1d9"));

        if (npc.Specializations.Count > 0)
        {
            panel.Children.Add(CreateInfoRow("Специализации:", BuildSpecializationsString(npc.Specializations), "#56d364"));
        }

        panel.Children.Add(CreateSectionHeader("СОЦИАЛЬНЫЕ СТАТЫ"));
        AddSocialStats(panel, npc);

        return panel;
    }
}