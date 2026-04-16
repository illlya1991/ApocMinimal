using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;

namespace ApocMinimal.Services;

public static class NpcInfoBuilder
{
    public static string GetStatColor(int value) =>
        value >= 75 ? "#4ade80" : value >= 50 ? "#c9d1d9" : "#fbbf24";

    public static TextBlock CreateSectionHeader(string title)
    {
        return new TextBlock
        {
            Text = title,
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#60a5fa")!,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 10, 0, 5)
        };
    }

    public static Grid CreateInfoRow(string label, string value, string colorHex)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120, GridUnitType.Pixel) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var labelBlock = new TextBlock
        {
            Text = label,
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#8b949e")!,
            FontSize = 12,
            Margin = new Thickness(0, 2, 0, 2)
        };
        var valueBlock = new TextBlock
        {
            Text = value,
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(colorHex)!,
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
        panel.Children.Add(new TextBlock
        {
            Text = "ФИЗИЧЕСКИЕ:",
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#60a5fa")!
        });
        var stats = npc.Stats.GetPhysicalStats();
        for (int i = 0; i < stats.Count; i++)
            panel.Children.Add(CreateInfoRow($"  {stats[i].Name}:", $"{stats[i]}", GetStatColor(stats[i].FinalValue)));
    }

    public static void AddMentalStats(StackPanel panel, Npc npc)
    {
        panel.Children.Add(new TextBlock
        {
            Text = "МЕНТАЛЬНЫЕ:",
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#60a5fa")!,
            Margin = new Thickness(0, 10, 0, 0)
        });
        var stats = npc.Stats.GetMentalStats();
        for (int i = 0; i < stats.Count; i++)
            panel.Children.Add(CreateInfoRow($"  {stats[i].Name}:", $"{stats[i]}", GetStatColor(stats[i].FinalValue)));
    }

    public static void AddEnergyStats(StackPanel panel, Npc npc)
    {
        panel.Children.Add(new TextBlock
        {
            Text = "ЭНЕРГЕТИЧЕСКИЕ:",
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#60a5fa")!,
            Margin = new Thickness(0, 10, 0, 0)
        });
        var stats = npc.Stats.GetEnergyStats();
        for (int i = 0; i < stats.Count; i++)
            panel.Children.Add(CreateInfoRow($"  {stats[i].Name}:", $"{stats[i]}", GetStatColor(stats[i].FinalValue)));
    }

    public static void AddCombatStats(StackPanel panel, Npc npc)
    {
        var stats = npc.Stats.GetCombatStats();
        for (int i = 0; i < stats.Count; i++)
            panel.Children.Add(CreateInfoRow($"  {stats[i].Name}:", $"{stats[i].FinalValue}", GetStatColor(stats[i].FinalValue)));
        panel.Children.Add(CreateInfoRow("  Боевая инициатива:", $"{npc.CombatInitiative:F0}", "#c9d1d9"));
    }

    public static void AddSocialStats(StackPanel panel, Npc npc)
    {
        var stats = npc.Stats.GetSocialStats();
        for (int i = 0; i < stats.Count; i++)
            panel.Children.Add(CreateInfoRow($"  {stats[i].Name}:", $"{stats[i].FinalValue}", GetStatColor(stats[i].FinalValue)));
    }

    public static void AddAllStats(StackPanel panel, Npc npc)
    {
        AddPhysicalStats(panel, npc);
        AddMentalStats(panel, npc);
        AddEnergyStats(panel, npc);
    }

    public static string BuildTraitsString(List<CharacterTrait> traits)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < traits.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(traits[i].ToLabel());
        }
        return sb.ToString();
    }

    public static string BuildEmotionsString(List<Emotion> emotions)
    {
        var sb = new System.Text.StringBuilder();
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
