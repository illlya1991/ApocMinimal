using System.Windows;
using System.Windows.Controls;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.StatisticsData;

namespace ApocMinimal.Services;

public static partial class NpcInfoBuilder
{
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

    public static void AddStatsByTypeCompare(StackPanel panel, Npc npc, Npc other, StatType type, bool hlSame, bool hlDiff)
    {
        List<Characteristic> stats = npc.Stats.GetStatsByType(type);
        List<Characteristic> otherStats = other.Stats.GetStatsByType(type);
        for (int i = 0; i < stats.Count; i++)
        {
            Characteristic stat = stats[i];
            double otherVal = i < otherStats.Count ? otherStats[i].FinalValue : 0;
            bool areSame = Math.Abs(stat.FinalValue - otherVal) <= 5;
            string? bg = null;
            if (areSame && hlSame) bg = "#0d2a0d";
            else if (!areSame && hlDiff) bg = "#2a0d0d";
            string display = stat.FinalValue == otherVal
                ? stat.FinalValue.ToString("F0")
                : stat.FinalValue > otherVal
                    ? $"{stat.FinalValue:F0} (▲{stat.FinalValue - otherVal:+0;+0})"
                    : $"{stat.FinalValue:F0} (▼{stat.FinalValue - otherVal:+0;+0})";
            panel.Children.Add(CreateInfoRow(stat.Name + ":", display, GetStatColor(stat.FinalValue), 0, bg));
        }
    }

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
        panel.Children.Add(CreateSectionHeader("ФИЗИЧЕСКИЕ", true));
        AddDetailedStatGroup(panel, npc.Stats.GetStatsByType(StatType.Physical));
        panel.Children.Add(CreateSectionHeader("МЕНТАЛЬНЫЕ", true));
        AddDetailedStatGroup(panel, npc.Stats.GetStatsByType(StatType.Mental));
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

            panel.Children.Add(new System.Windows.Controls.TextBlock
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

            panel.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "      База: " + stat.BaseValue.ToString().PadLeft(3) +
                       " | Откл.: " + deviationText.PadLeft(3) +
                       " | Полн.база: " + stat.FullBase.ToString().PadLeft(3) +
                       " | Итог: " + stat.FinalValue.ToString().PadLeft(3),
                Foreground = GetStatColorBrush(stat.FinalValue),
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 2)
            });

            List<PermanentModifier> permMods = stat.GetModifiersByType<PermanentModifier>();
            for (int j = 0; j < permMods.Count; j++)
            {
                PermanentModifier mod = permMods[j];
                if (mod.IsActive())
                {
                    string modSign = (mod.Type == ModifierType.Additive) ? "+" : "×";
                    panel.Children.Add(new System.Windows.Controls.TextBlock
                    {
                        Text = "        [П] " + mod.Name + ": " + modSign + mod.Value.ToString() + " (" + mod.Source + ")",
                        Foreground = GetBrush("#facc15"),
                        FontSize = 10,
                        Margin = new Thickness(0, 0, 0, 1)
                    });
                }
            }

            List<IndependentModifier> indMods = stat.GetModifiersByType<IndependentModifier>();
            for (int j = 0; j < indMods.Count; j++)
            {
                IndependentModifier mod = indMods[j];
                if (mod.IsActive())
                {
                    string modSign = (mod.Type == ModifierType.Additive) ? "+" : "×";
                    string timeLeft = mod.TimeUnit switch
                    {
                        TimeUnit.Days        => $" (ост. {mod.Remaining} дн.)",
                        TimeUnit.Hours       => $" (ост. {mod.Remaining} ч.)",
                        TimeUnit.CombatTurns => $" (ост. {mod.Remaining} ходов)",
                        _                    => ""
                    };
                    panel.Children.Add(new System.Windows.Controls.TextBlock
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
}
