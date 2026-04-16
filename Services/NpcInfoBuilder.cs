// NpcInfoBuilder.cs - полный исправленный файл

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.StatisticsData;

namespace ApocMinimal.Services;

public static class NpcInfoBuilder
{
    public static string GetStatColor(int value)
    {
        if (value >= 75) return "#4ade80";
        if (value >= 50) return "#c9d1d9";
        return "#fbbf24";
    }

    private static SolidColorBrush? GetStatColorBrush(int value)
    {
        string hex = GetStatColor(value);
        return GetColorBrush(hex);
    }
    private static SolidColorBrush? GetColorBrush(string hex)
    {
        return new BrushConverter().ConvertFromString(hex) as SolidColorBrush;
    }

    public static TextBlock CreateSectionHeader(string title)
    {
        return new TextBlock
        {
            Text = title,
            Foreground = GetColorBrush("#60a5fa"),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 10, 0, 5)
        };
    }

    public static Grid CreateInfoRow(string label, string value, string colorHex)
    {
        Grid grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120, GridUnitType.Pixel) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        TextBlock labelBlock = new TextBlock
        {
            Text = label,
            Foreground = GetColorBrush("#8b949e"),
            FontSize = 12,
            Margin = new Thickness(0, 2, 0, 2)
        };

        TextBlock valueBlock = new TextBlock
        {
            Text = value,
            Foreground = GetColorBrush(colorHex),
            FontSize = 12,
            Margin = new Thickness(5, 2, 0, 2)
        };

        grid.Children.Add(labelBlock);
        grid.Children.Add(valueBlock);
        Grid.SetColumn(valueBlock, 1);
        return grid;
    }

    public static void AddPhysicalStats(StackPanel panel, Npc npc)
    {
        List<Characteristic> stats = npc.Stats.GetPhysicalStats();
        for (int i = 0; i < stats.Count; i++)
        {
            panel.Children.Add(CreateInfoRow(stats[i].Name + ":", stats[i].FinalValue.ToString(), GetStatColor(stats[i].FinalValue)));
        }
    }

    public static void AddMentalStats(StackPanel panel, Npc npc)
    {
        List<Characteristic> stats = npc.Stats.GetMentalStats();
        for (int i = 0; i < stats.Count; i++)
        {
            panel.Children.Add(CreateInfoRow(stats[i].Name + ":", stats[i].FinalValue.ToString(), GetStatColor(stats[i].FinalValue)));
        }
    }

    public static void AddEnergyStats(StackPanel panel, Npc npc)
    {
        List<Characteristic> stats = npc.Stats.GetEnergyStats();
        for (int i = 0; i < stats.Count; i++)
        {
            panel.Children.Add(CreateInfoRow(stats[i].Name + ":", stats[i].FinalValue.ToString(), GetStatColor(stats[i].FinalValue)));
        }
    }

    public static void AddCombatStats(StackPanel panel, Npc npc)
    {
        List<Characteristic> stats = npc.Stats.GetCombatStats();
        for (int i = 0; i < stats.Count; i++)
        {
            panel.Children.Add(CreateInfoRow(stats[i].Name + ":", stats[i].FinalValue.ToString(), GetStatColor(stats[i].FinalValue)));
        }
        panel.Children.Add(CreateInfoRow("Боевая инициатива:", npc.CombatInitiative.ToString("F0"), "#c9d1d9"));
    }

    public static void AddSocialStats(StackPanel panel, Npc npc)
    {
        List<Characteristic> stats = npc.Stats.GetSocialStats();
        for (int i = 0; i < stats.Count; i++)
        {
            panel.Children.Add(CreateInfoRow(stats[i].Name + ":", stats[i].FinalValue.ToString(), GetStatColor(stats[i].FinalValue)));
        }
    }

    public static void AddAllStats(StackPanel panel, Npc npc)
    {
        AddPhysicalStats(panel, npc);
        AddMentalStats(panel, npc);
        AddEnergyStats(panel, npc);
    }

    public static void AddDetailedStats(StackPanel panel, Npc npc)
    {
        // Физические
        panel.Children.Add(CreateSectionHeader("ФИЗИЧЕСКИЕ"));
        List<Characteristic> physicalStats = npc.Stats.GetPhysicalStats();
        for (int i = 0; i < physicalStats.Count; i++)
        {
            Characteristic stat = physicalStats[i];

            string combatIcon = "";
            string socialIcon = "";
            if (stat.IsCombat) combatIcon = "⚔ ";
            if (stat.IsSocial) socialIcon = "💬 ";

            TextBlock nameBlock = new TextBlock();
            nameBlock.Text = "  " + combatIcon + socialIcon + stat.Name + ":";
            nameBlock.Foreground = GetColorBrush("#8b949e");
            nameBlock.FontSize = 11;
            nameBlock.Margin = new Thickness(0, 4, 0, 2);
            panel.Children.Add(nameBlock);

            string deviationStr = (stat.Deviation >= 0) ? "+" + stat.Deviation.ToString() : stat.Deviation.ToString();
            TextBlock valueBlock = new TextBlock();
            valueBlock.Text = "      База: " + stat.BaseValue + " | Откл.: " + deviationStr + " | Полн.база: " + stat.FullBase + " | Итог: " + stat.FinalValue;
            valueBlock.Foreground = GetStatColorBrush(stat.FinalValue);
            valueBlock.FontSize = 11;
            valueBlock.Margin = new Thickness(0, 0, 0, 2);
            panel.Children.Add(valueBlock);

            List<PermanentModifier> permMods = stat.GetModifiersByType<PermanentModifier>();
            for (int j = 0; j < permMods.Count; j++)
            {
                PermanentModifier mod = permMods[j];
                if (mod.IsActive())
                {
                    string modSign = (mod.Type == ModifierType.Additive) ? "+" : "x";
                    TextBlock modBlock = new TextBlock();
                    modBlock.Text = "        [П] " + mod.Name + ": " + modSign + mod.Value + " (" + mod.Source + ")";
                    modBlock.Foreground = GetColorBrush("#facc15");
                    modBlock.FontSize = 10;
                    modBlock.Margin = new Thickness(0, 0, 0, 1);
                    panel.Children.Add(modBlock);
                }
            }

            List<IndependentModifier> indMods = stat.GetModifiersByType<IndependentModifier>();
            for (int j = 0; j < indMods.Count; j++)
            {
                IndependentModifier mod = indMods[j];
                if (mod.IsActive())
                {
                    string modSign = (mod.Type == ModifierType.Additive) ? "+" : "x";
                    string timeLeft = "";
                    if (mod.TimeUnit == TimeUnit.Days)
                    {
                        timeLeft = " (ост. " + mod.Remaining + " дн.)";
                    }
                    else if (mod.TimeUnit == TimeUnit.Hours)
                    {
                        timeLeft = " (ост. " + mod.Remaining + " ч.)";
                    }
                    else if (mod.TimeUnit == TimeUnit.CombatTurns)
                    {
                        timeLeft = " (ост. " + mod.Remaining + " ходов)";
                    }
                    TextBlock modBlock = new TextBlock();
                    modBlock.Text = "        [В] " + mod.Name + ": " + modSign + mod.Value + timeLeft + " (" + mod.Source + ")";
                    modBlock.Foreground = GetColorBrush("#fbbf24");
                    modBlock.FontSize = 10;
                    modBlock.Margin = new Thickness(0, 0, 0, 1);
                    panel.Children.Add(modBlock);
                }
            }
        }

        // Ментальные
        panel.Children.Add(CreateSectionHeader("МЕНТАЛЬНЫЕ"));
        List<Characteristic> mentalStats = npc.Stats.GetMentalStats();
        for (int i = 0; i < mentalStats.Count; i++)
        {
            Characteristic stat = mentalStats[i];

            string combatIcon = "";
            string socialIcon = "";
            if (stat.IsCombat) combatIcon = "⚔ ";
            if (stat.IsSocial) socialIcon = "💬 ";

            TextBlock nameBlock = new TextBlock();
            nameBlock.Text = "  " + combatIcon + socialIcon + stat.Name + ":";
            nameBlock.Foreground = GetColorBrush("#8b949e");
            nameBlock.FontSize = 11;
            nameBlock.Margin = new Thickness(0, 4, 0, 2);
            panel.Children.Add(nameBlock);

            string deviationStr = (stat.Deviation >= 0) ? "+" + stat.Deviation.ToString() : stat.Deviation.ToString();
            TextBlock valueBlock = new TextBlock();
            valueBlock.Text = "      База: " + stat.BaseValue + " | Откл.: " + deviationStr + " | Полн.база: " + stat.FullBase + " | Итог: " + stat.FinalValue;
            valueBlock.Foreground = GetStatColorBrush(stat.FinalValue);
            valueBlock.FontSize = 11;
            valueBlock.Margin = new Thickness(0, 0, 0, 2);
            panel.Children.Add(valueBlock);

            List<PermanentModifier> permMods = stat.GetModifiersByType<PermanentModifier>();
            for (int j = 0; j < permMods.Count; j++)
            {
                PermanentModifier mod = permMods[j];
                if (mod.IsActive())
                {
                    string modSign = (mod.Type == ModifierType.Additive) ? "+" : "x";
                    TextBlock modBlock = new TextBlock();
                    modBlock.Text = "        [П] " + mod.Name + ": " + modSign + mod.Value + " (" + mod.Source + ")";
                    modBlock.Foreground = GetColorBrush("#facc15");
                    modBlock.FontSize = 10;
                    modBlock.Margin = new Thickness(0, 0, 0, 1);
                    panel.Children.Add(modBlock);
                }
            }

            List<IndependentModifier> indMods = stat.GetModifiersByType<IndependentModifier>();
            for (int j = 0; j < indMods.Count; j++)
            {
                IndependentModifier mod = indMods[j];
                if (mod.IsActive())
                {
                    string modSign = (mod.Type == ModifierType.Additive) ? "+" : "x";
                    string timeLeft = "";
                    if (mod.TimeUnit == TimeUnit.Days)
                    {
                        timeLeft = " (ост. " + mod.Remaining + " дн.)";
                    }
                    else if (mod.TimeUnit == TimeUnit.Hours)
                    {
                        timeLeft = " (ост. " + mod.Remaining + " ч.)";
                    }
                    else if (mod.TimeUnit == TimeUnit.CombatTurns)
                    {
                        timeLeft = " (ост. " + mod.Remaining + " ходов)";
                    }
                    TextBlock modBlock = new TextBlock();
                    modBlock.Text = "        [В] " + mod.Name + ": " + modSign + mod.Value + timeLeft + " (" + mod.Source + ")";
                    modBlock.Foreground = GetColorBrush("#fbbf24");
                    modBlock.FontSize = 10;
                    modBlock.Margin = new Thickness(0, 0, 0, 1);
                    panel.Children.Add(modBlock);
                }
            }
        }

        // Энергетические
        panel.Children.Add(CreateSectionHeader("ЭНЕРГЕТИЧЕСКИЕ"));
        List<Characteristic> energyStats = npc.Stats.GetEnergyStats();
        for (int i = 0; i < energyStats.Count; i++)
        {
            Characteristic stat = energyStats[i];

            TextBlock nameBlock = new TextBlock();
            nameBlock.Text = "  " + stat.Name + ":";
            nameBlock.Foreground = GetColorBrush("#8b949e");
            nameBlock.FontSize = 11;
            nameBlock.Margin = new Thickness(0, 4, 0, 2);
            panel.Children.Add(nameBlock);

            string deviationStr = (stat.Deviation >= 0) ? "+" + stat.Deviation.ToString() : stat.Deviation.ToString();
            TextBlock valueBlock = new TextBlock();
            valueBlock.Text = "      База: " + stat.BaseValue + " | Откл.: " + deviationStr + " | Полн.база: " + stat.FullBase + " | Итог: " + stat.FinalValue;
            valueBlock.Foreground = GetStatColorBrush(stat.FinalValue);
            valueBlock.FontSize = 11;
            valueBlock.Margin = new Thickness(0, 0, 0, 2);
            panel.Children.Add(valueBlock);

            List<PermanentModifier> permMods = stat.GetModifiersByType<PermanentModifier>();
            for (int j = 0; j < permMods.Count; j++)
            {
                PermanentModifier mod = permMods[j];
                if (mod.IsActive())
                {
                    string modSign = (mod.Type == ModifierType.Additive) ? "+" : "x";
                    TextBlock modBlock = new TextBlock();
                    modBlock.Text = "        [П] " + mod.Name + ": " + modSign + mod.Value + " (" + mod.Source + ")";
                    modBlock.Foreground = GetColorBrush("#facc15");
                    modBlock.FontSize = 10;
                    modBlock.Margin = new Thickness(0, 0, 0, 1);
                    panel.Children.Add(modBlock);
                }
            }

            List<IndependentModifier> indMods = stat.GetModifiersByType<IndependentModifier>();
            for (int j = 0; j < indMods.Count; j++)
            {
                IndependentModifier mod = indMods[j];
                if (mod.IsActive())
                {
                    string modSign = (mod.Type == ModifierType.Additive) ? "+" : "x";
                    string timeLeft = "";
                    if (mod.TimeUnit == TimeUnit.Days)
                    {
                        timeLeft = " (ост. " + mod.Remaining + " дн.)";
                    }
                    else if (mod.TimeUnit == TimeUnit.Hours)
                    {
                        timeLeft = " (ост. " + mod.Remaining + " ч.)";
                    }
                    else if (mod.TimeUnit == TimeUnit.CombatTurns)
                    {
                        timeLeft = " (ост. " + mod.Remaining + " ходов)";
                    }
                    TextBlock modBlock = new TextBlock();
                    modBlock.Text = "        [В] " + mod.Name + ": " + modSign + mod.Value + timeLeft + " (" + mod.Source + ")";
                    modBlock.Foreground = GetColorBrush("#fbbf24");
                    modBlock.FontSize = 10;
                    modBlock.Margin = new Thickness(0, 0, 0, 1);
                    panel.Children.Add(modBlock);
                }
            }
        }
    }

    public static string BuildTraitsString(List<CharacterTrait> traits)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < traits.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(traits[i].ToLabel());
        }
        return sb.ToString();
    }

    public static string BuildEmotionsString(List<Emotion> emotions)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
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
}