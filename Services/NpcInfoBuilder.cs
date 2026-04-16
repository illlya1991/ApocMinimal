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

    public static Grid CreateInfoRow(string label, string value, string colorHex, double leftMargin = 0)
    {
        Grid grid = new Grid();
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

        // Срочные потребности
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

        // Характеристики (сворачиваемые)
        Expander statsExpander = CreateCollapsibleSection("ХАРАКТЕРИСТИКИ", true);
        StackPanel statsPanel = new StackPanel { Margin = new Thickness(15, 0, 0, 0) };
        if (!isCompact)
            AddAllStats(statsPanel, npc);
        else
            AddDetailedStats(statsPanel, npc);
        statsExpander.Content = statsPanel;
        panel.Children.Add(statsExpander);

        return panel;
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