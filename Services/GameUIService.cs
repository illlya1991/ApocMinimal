using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ApocMinimal.Systems;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.StatisticsData;

namespace ApocMinimal.Services;

public class GameUIService
{
    private readonly Action<string, string> _logAction;

    public GameUIService(Action<string, string> logAction)
    {
        _logAction = logAction;
    }

    public Border BuildNpcCard(Npc npc, Func<Npc, double>? devGenFunc = null)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var card = new Border
        {
            Background = GetStatusBrush(npc),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(0, 0, 0, 6),
            Padding = new Thickness(8, 6, 8, 6),
            Opacity = npc.IsAlive ? 1.0 : 0.5,
            Cursor = System.Windows.Input.Cursors.Hand,
        };

        var panel = new StackPanel();

        // Name row with follower level badge
        var nameRow = CreateNameRow(npc);
        panel.Children.Add(nameRow);

        // Добавляем уровень последователя как бейдж
        if (npc.FollowerLevel > 0)
        {
            double devGen = devGenFunc?.Invoke(npc) ?? 0;
            var levelBadge = CreateFollowerLevelBadge(npc.FollowerLevel, devGen);
            panel.Children.Add(levelBadge);
        }

        if (!npc.IsAlive)
        {
            card.Child = panel;
            return card;
        }

        // Health bars
        panel.Children.Add(CreateHealthBar("HP", npc.Health, npc.Health < 30 ? "#f87171" : "#4ade80"));
        panel.Children.Add(CreateHealthBar("Выносл.", npc.Stamina, npc.Stamina < 30 ? "#f87171" : "#60a5fa"));

        // Stats row
        panel.Children.Add(CreateStatsText(npc));
        panel.Children.Add(CreateProfessionText(npc));

        if (npc.CharTraits.Any())
            panel.Children.Add(CreateTraitsText(npc));

        var urgent = NeedSystem.GetMostUrgentNeed(npc);
        if (urgent != null)
            panel.Children.Add(CreateUrgentNeedText(urgent));

        if (npc.HasTask)
            panel.Children.Add(CreateTaskText(npc));

        card.Child = panel;

        if (sw.ElapsedMilliseconds > 5)
            System.Diagnostics.Debug.WriteLine($"        BuildNpcCard для {npc.Name}: {sw.ElapsedMilliseconds} мс");

        return card;
    }
    private static Border CreateFollowerLevelBadge(int level, double devGen = 0)
    {
        string levelName = level switch
        {
            1 => "Послушник",
            2 => "Последователь",
            3 => "Верный",
            4 => "Преданный",
            5 => "Фанатик",
            _ => $"Ур.{level}"
        };

        string levelColor = level switch
        {
            1 => "#8b949e",
            2 => "#56d364",
            3 => "#e3b341",
            4 => "#f97316",
            5 => "#f87171",
            _ => "#8b949e"
        };

        string text = devGen > 0
            ? $"{levelName} ({level})  •  {devGen:F1} ОР/д"
            : $"{levelName} ({level})";

        return new Border
        {
            Background = BrushCache.GetBrush("#1a1a2e"),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(0, 2, 0, 4),
            Child = new TextBlock
            {
                Text = text,
                Foreground = BrushCache.GetBrush(levelColor),
                FontSize = 9,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };
    }
    public Expander CreateDayExpander(string header, bool isExpanded = true)
    {
        return new Expander
        {
            Header = header,
            IsExpanded = isExpanded,
            Style = GetDayExpanderStyle()
        };
    }

    public TextBlock CreateLogEntry(string text, string color, bool isAlert = false)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = GetBrush(color),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 1, 0, 0),
            FontWeight = isAlert ? FontWeights.Bold : FontWeights.Normal,
        };
    }

    private static Grid CreateNameRow(Npc npc)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var nameText = new TextBlock
        {
            Text = npc.IsAlive ? $"{npc.Name} ({npc.GenderLabel})" : $"[погиб] {npc.Name}",
            Foreground = Brushes.White,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
        };
        Grid.SetColumn(nameText, 0);

        var traitText = new TextBlock
        {
            Text = npc.TraitLabel,
            Foreground = GetTraitBrush(npc.Trait),
            FontSize = 10,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(traitText, 1);

        grid.Children.Add(nameText);
        grid.Children.Add(traitText);
        return grid;
    }

    private static StackPanel CreateHealthBar(string label, double value, string colorHex)
    {
        const double maxWidth = 220;
        var panel = new StackPanel();

        panel.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = GetBrush("#8b949e"),
            FontSize = 9,
            Margin = new Thickness(0, 2, 0, 0)
        });

        var bg = new Border { Background = GetBrush("#1a3a1a"), CornerRadius = new CornerRadius(2), Height = 7 };
        var fill = new Border
        {
            Background = GetBrush(colorHex),
            CornerRadius = new CornerRadius(2),
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = Math.Max(0, Math.Min(1, value / 100.0)) * maxWidth,
        };

        var grid = new Grid();
        grid.Children.Add(bg);
        grid.Children.Add(fill);
        panel.Children.Add(grid);

        return panel;
    }


    private static TextBlock CreateProfessionText(Npc npc) => new()
    {
        Text = $"{npc.Profession}  [{npc.FollowerLabel}]",
        Foreground = GetBrush("#8b949e"),
        FontSize = 10,
    };

    private static TextBlock CreateTraitsText(Npc npc) => new()
    {
        Text = string.Join(", ", npc.CharTraits.Select(c => c.ToLabel())),
        Foreground = GetBrush("#d29922"),
        FontSize = 10,
    };

    private static TextBlock CreateUrgentNeedText(Need need) => new()
    {
        Text = $"Нужда: {need.Name} ({need.Value:F0}%)",
        Foreground = GetBrush(need.IsCritical ? "#f87171" : "#fbbf24"),
        FontSize = 10,
    };

    private static TextBlock CreateTaskText(Npc npc) => new()
    {
        Text = $"Задание: {npc.ActiveTask} ({npc.TaskDaysLeft} дн.)",
        Foreground = GetBrush("#e879f9"),
        FontSize = 10,
    };

    private static SolidColorBrush GetBrush(string hex) => BrushCache.GetBrush(hex);

    private static SolidColorBrush GetStatusBrush(Npc npc) => GetBrush(npc.StatusColor);

    private static SolidColorBrush GetTraitBrush(NpcTrait trait) => trait switch
    {
        NpcTrait.Leader => GetBrush("#facc15"),
        NpcTrait.Coward => GetBrush("#f87171"),
        NpcTrait.Loner => GetBrush("#94a3b8"),
        _ => GetBrush("#161b22"),
    };

    private static Style GetDayExpanderStyle()
    {
        var style = new Style(typeof(Expander));
        style.Setters.Add(new Setter(Expander.BackgroundProperty, GetBrush("#0d1117")));
        style.Setters.Add(new Setter(Expander.ForegroundProperty, GetBrush("#60a5fa")));
        return style;
    }

    // Добавьте эти методы в GameUIService.cs

    public Border BuildQuestCard(Quest quest, bool isActive, string? executorName = null)
    {
        var b = new Border
        {
            Background = GetBrush(isActive ? "#1a2a1a" : "#161b22"),
            BorderBrush = GetBrush("#30363d"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            Margin = new Thickness(0, 0, 0, 4),
            Padding = new Thickness(6, 4, 6, 4),
        };

        var sp = new StackPanel();

        sp.Children.Add(new TextBlock
        {
            Text = quest.Title,
            Foreground = GetBrush(isActive ? "#56d364" : "#c9d1d9"),
            FontWeight = FontWeights.SemiBold
        });

        sp.Children.Add(new TextBlock
        {
            Text = quest.Description,
            Foreground = GetBrush("#8b949e"),
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap
        });

        if (isActive && executorName != null)
        {
            sp.Children.Add(new TextBlock
            {
                Text = $"Исполнитель: {executorName}",
                Foreground = GetBrush("#8b949e"),
                FontSize = 10
            });
            sp.Children.Add(new TextBlock
            {
                Text = $"Осталось дней: {quest.DaysRemaining}",
                Foreground = GetBrush("#d29922"),
                FontSize = 10
            });
        }
        else
        {
            sp.Children.Add(new TextBlock
            {
                Text = $"Дней: {quest.DaysRequired}  Вознаграждение: {quest.RewardAmount:F0} ед.",
                Foreground = GetBrush("#d29922"),
                FontSize = 10
            });
        }

        b.Child = sp;
        return b;
    }

    /// <summary>
    /// Создать панель со всеми характеристиками через циклы
    /// </summary>
    public StackPanel BuildStatsPanel(Npc npc, bool showAll = true)
    {
        var panel = new StackPanel();

        if (showAll)
        {
            AddStatsCategory(panel, npc.Stats.GetStatsByType(StatType.Physical), "ФИЗИЧЕСКИЕ");
            AddStatsCategory(panel, npc.Stats.GetStatsByType(StatType.Physical), "МЕНТАЛЬНЫЕ");
            AddStatsCategory(panel, npc.Stats.GetStatsByType(StatType.Physical), "ЭНЕРГЕТИЧЕСКИЕ");
        }

        return panel;
    }

    /// <summary>
    /// Добавить категорию характеристик в панель
    /// </summary>
    private void AddStatsCategory(StackPanel panel, List<Characteristic> stats, string categoryName)
    {
        if (!stats.Any()) return;

        panel.Children.Add(new TextBlock
        {
            Text = categoryName,
            Foreground = GetBrush("#60a5fa"),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 10, 0, 5)
        });

        foreach (var stat in stats)
        {
            panel.Children.Add(CreateStatRow(stat.Name, stat.FinalValue, stat.FullBase));
        }
    }

    /// <summary>
    /// Создать строку характеристики
    /// </summary>
    private Grid CreateStatRow(string name, int finalValue, int fullBase)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140, GridUnitType.Pixel) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        grid.Children.Add(new TextBlock
        {
            Text = name + ":",
            Foreground = GetBrush("#8b949e"),
            FontSize = 11,
            Margin = new Thickness(0, 1, 0, 1)
        });

        string color = GetStatColor(finalValue);
        grid.Children.Add(new TextBlock
        {
            Text = $"{finalValue,3}  (база: {fullBase})",
            Foreground = GetBrush(color),
            FontSize = 11,
            Margin = new Thickness(5, 1, 0, 1)
        });

        Grid.SetColumn(grid.Children[1], 1);
        return grid;
    }

    /// <summary>
    /// Получить цвет для значения характеристики
    /// </summary>
    private string GetStatColor(int value)
    {
        if (value >= 75) return "#4ade80";
        if (value >= 50) return "#c9d1d9";
        return "#fbbf24";
    }
    private static TextBlock CreateStatsText(Npc npc) => new()
    {
        Text = $"Сил:{npc.Stats.GetStatValue("Сила")}  Лов:{npc.Stats.GetStatValue("Ловкость")}  Инт:{npc.Stats.GetStatValue("Интеллект")}",
        Foreground = GetBrush("#8b949e"),
        FontSize = 10,
    };
}