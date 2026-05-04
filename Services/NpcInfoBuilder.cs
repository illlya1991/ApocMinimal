using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.StatisticsData;
using ApocMinimal.Models.TechniqueData;

namespace ApocMinimal.Services;

public static partial class NpcInfoBuilder
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
        return BrushCache.GetBrush(hex)!;
    }

    private static SolidColorBrush GetBrush(string hex) => BrushCache.GetBrush(hex);

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
        panel.Children.Add(CreateInfoRow("Энергия:", NumValue(npc.Energy, other.Energy), "#e879f9", 0, N(npc.Energy, other.Energy)));
        panel.Children.Add(CreateInfoRow("Преданность:", NumValue(npc.Devotion, other.Devotion), "#facc15", 0, N(npc.Devotion, other.Devotion)));
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

        panel.Children.Add(new TextBlock
        {
            Text = $"{npc.Name} [{npc.GenderLabel}] {npc.Age} лет",
            FontSize = isCompact ? 14 : 16,
            FontWeight = FontWeights.Bold,
            Foreground = GetBrush("#60a5fa"),
            Margin = new Thickness(0, 0, 0, isCompact ? 8 : 10)
        });

        panel.Children.Add(CreateSectionHeader("ОСНОВНЫЕ ХАРАКТЕРИСТИКИ"));
        panel.Children.Add(CreateInfoRow("HP:", $"{npc.Health:F0}", npc.Health < 30 ? "#f87171" : "#4ade80"));
        panel.Children.Add(CreateInfoRow("Выносливость:", $"{npc.Stamina:F0}", npc.Stamina < 30 ? "#f87171" : "#60a5fa"));
        panel.Children.Add(CreateInfoRow("Энергия:", $"{npc.Energy:F0}", "#e879f9"));
        panel.Children.Add(CreateInfoRow("Преданность:", $"{npc.Devotion:F0}", "#facc15"));
        panel.Children.Add(CreateInfoRow("Страх:", $"{npc.Fear:F0}", npc.Fear > 70 ? "#f87171" : "#c9d1d9"));
        panel.Children.Add(CreateInfoRow("Доверие:", $"{npc.Trust:F0}", npc.Trust > 70 ? "#4ade80" : "#c9d1d9"));
        panel.Children.Add(CreateInfoRow("Инициатива:", $"{npc.Initiative:F0}", "#c9d1d9"));
        panel.Children.Add(CreateInfoRow("Уровень:", npc.FollowerLabel, "#d29922"));

        panel.Children.Add(CreateSectionHeader("ЛИЧНОСТЬ"));
        panel.Children.Add(CreateInfoRow("Черты:", BuildTraitsString(npc.CharTraits), "#d29922"));
        panel.Children.Add(CreateInfoRow("Эмоции:", BuildEmotionsString(npc.Emotions), "#e879f9"));

        panel.Children.Add(CreateSectionHeader("ЦЕЛИ"));
        panel.Children.Add(CreateInfoRow("Цель:", npc.Goal, "#c9d1d9"));
        panel.Children.Add(CreateInfoRow("Мечта:", npc.Dream, "#c9d1d9"));
        panel.Children.Add(CreateInfoRow("Желание:", npc.Desire, "#c9d1d9"));

        if (npc.Specializations.Count > 0)
            panel.Children.Add(CreateInfoRow("Специализации:", BuildSpecializationsString(npc.Specializations), "#56d364"));

        AddTechAbilitySection(panel, npc);

        if (isCompact)
        {
            AddAllNeeds(panel, npc);
        }
        else
        {
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
                panel.Children.Add(new TextBlock { Text = "  Нет срочных потребностей", Foreground = GetBrush("#4ade80"), FontSize = 11, Margin = new Thickness(0, 2, 0, 2) });
        }

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
        if (npc.LearnedTechIds.Count == 0) return;

        panel.Children.Add(CreateSectionHeader("ТЕХНИКИ"));
        foreach (var key in npc.LearnedTechIds)
        {
            panel.Children.Add(new TextBlock
            {
                Text = $"▸ {key}",
                Foreground = GetBrush("#79c0ff"),
                FontSize = 12,
                Margin = new Thickness(0, 2, 0, 2),
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
            Text = $"❤{npc.Health:F0} ✦{npc.Devotion:F0} 😨{npc.Fear:F0} 🤝{npc.Trust:F0} 💪{npc.Stats.Strength} 🧠{npc.Stats.Intelligence}",
            Foreground = GetBrush("#c9d1d9"),
            FontSize = 12,
            Margin = new Thickness(0, 5, 0, 5)
        });

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
        panel.Children.Add(CreateInfoRow("Преданность:", $"{npc.Devotion:F0}", "#facc15"));
        panel.Children.Add(CreateInfoRow("Уровень последователя:", npc.FollowerLabel, "#d29922"));
        panel.Children.Add(CreateInfoRow("Черты:", BuildTraitsString(npc.CharTraits), "#d29922"));
        panel.Children.Add(CreateInfoRow("Эмоции:", BuildEmotionsString(npc.Emotions), "#e879f9"));
        panel.Children.Add(CreateInfoRow("Цель:", npc.Goal, "#c9d1d9"));
        panel.Children.Add(CreateInfoRow("Мечта:", npc.Dream, "#c9d1d9"));

        if (npc.Specializations.Count > 0)
            panel.Children.Add(CreateInfoRow("Специализации:", BuildSpecializationsString(npc.Specializations), "#56d364"));

        panel.Children.Add(CreateSectionHeader("СОЦИАЛЬНЫЕ СТАТЫ"));
        AddSocialStats(panel, npc);

        return panel;
    }
}
