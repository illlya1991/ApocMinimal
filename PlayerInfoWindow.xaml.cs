using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Services;
using ApocMinimal.ViewModels;

namespace ApocMinimal;

public partial class PlayerInfoWindow : Window
{
    private readonly GameViewModel _vm;

    public PlayerInfoWindow(GameViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        Populate();
    }

    private void Populate()
    {
        var player = _vm.GetPlayer();
        var factions = new ListPlayerFactions();
        var fInfo = factions.factions.FirstOrDefault(f => f.Faction == player.Faction)
                    ?? new OnePlayerFaction();

        // Header
        NameLabel.Text    = player.Name;
        FactionLabel.Text = fInfo.Label;
        FactionLabel.Foreground = BrushCache.GetBrush(fInfo.Color)!;
        FactionDesc.Text  = fInfo.Description;
        DayLabel.Text     = $"День {player.CurrentDay}";
        TerminalLabel.Text = $"Терминал: ур.{player.TerminalLevel}";

        int alive = _vm.AllNpcs.Count(n => n.IsAlive && n.FollowerLevel > 0);
        FollowersLabel.Text = $"Посл.: {alive}/{player.MaxActiveFollowers}";

        // Faction coefficients
        AddCoeffRow(CoeffPanel, "ОР за НПС",                   player.FactionCoeffs.CoeffDevPerNpc,         1.0, isMultiplier: true);
        AddCoeffRow(CoeffPanel, "ОР за локацию (за шт.)",      player.FactionCoeffs.CoeffDevPerLocation,    0.0, isMultiplier: false);
        AddCoeffRow(CoeffPanel, "Эффект пожертвования",         player.FactionCoeffs.CoeffDonation,          1.0, isMultiplier: true);
        AddCoeffRow(CoeffPanel, "Стоимость апгрейда Терм.",     player.FactionCoeffs.CoeffTerminalUpgradeCost, 1.0, isMultiplier: true, lowerIsBetter: true);
        AddCoeffRow(CoeffPanel, "Рост статов НПС",              player.FactionCoeffs.CoeffStatGrowth,        1.0, isMultiplier: true);
        AddCoeffRow(CoeffPanel, "Стоимость магазина",           player.FactionCoeffs.CoeffShopCost,          1.0, isMultiplier: true, lowerIsBetter: true);
        AddCoeffRow(CoeffPanel, "Макс. ОР/НПС/день",           player.FactionCoeffs.CoeffMaxDevPerNpc,      1.0, isMultiplier: true);
        AddCoeffRow(CoeffPanel, "Единицы барьера",              player.FactionCoeffs.CoeffBarrierUnits,      1.0, isMultiplier: true);

        // Follower limits
        var npcsByLevel = new int[6];
        foreach (var n in _vm.AllNpcs.Where(n => n.IsAlive))
            npcsByLevel[Math.Clamp(n.FollowerLevel, 0, 5)]++;

        double maxDev = Player.MaxDevPointsPerNpcPerDay * player.FactionCoeffs.CoeffMaxDevPerNpc;

        for (int lvl = 1; lvl <= 5; lvl++)
        {
            int limit = player.GetFollowerLimit(lvl);
            int count = npcsByLevel[lvl];
            string limitStr = limit < 0 ? "∞" : limit.ToString();

            double minOrMax = lvl * (maxDev / 5.0);
            string devRange = lvl == 1
                ? $"0–{minOrMax:F0}"
                : $"{(lvl - 1) * (maxDev / 5.0):F0}–{minOrMax:F0}";

            var row = new Grid { Margin = new Thickness(0, 1, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            string levelColor = lvl switch { 1 => "#8b949e", 2 => "#56d364", 3 => "#e3b341", 4 => "#f97316", 5 => "#f87171", _ => "#c9d1d9" };
            bool full = limit >= 0 && count >= limit;

            AddCell(row, 0, $"Ур.{lvl}", levelColor);
            AddCell(row, 1, count.ToString(), full ? "#f87171" : "#56d364");
            AddCell(row, 2, limitStr, "#8b949e");
            AddCell(row, 3, devRange, "#a5d6ff");

            FollowerLimitsPanel.Children.Add(row);
        }

    }

    private void AddCoeffRow(StackPanel panel, string label, double value, double neutral,
        bool isMultiplier, bool lowerIsBetter = false)
    {
        var grid = new Grid { Margin = new Thickness(0, 2, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        string display = isMultiplier ? $"×{value:F2}" : $"+{value:F1}/день";
        bool better = lowerIsBetter ? value < neutral : value > neutral;
        bool worse  = lowerIsBetter ? value > neutral : value < neutral;
        string color = better ? "#56d364" : worse ? "#f87171" : "#8b949e";

        var lbl = new TextBlock { Text = label, Foreground = Brushes.LightGray, FontSize = 11, Padding = new Thickness(4, 2, 0, 2) };
        var val = new TextBlock { Text = display, Foreground = BrushCache.GetBrush(color)!, FontSize = 11, Padding = new Thickness(4, 2, 4, 2), HorizontalAlignment = HorizontalAlignment.Right };

        Grid.SetColumn(lbl, 0);
        Grid.SetColumn(val, 1);
        grid.Children.Add(lbl);
        grid.Children.Add(val);
        panel.Children.Add(grid);
    }

    private static void AddCell(Grid row, int col, string text, string color)
    {
        var tb = new TextBlock
        {
            Text = text,
            Foreground = BrushCache.GetBrush(color)!,
            Padding = new Thickness(4, 2, 4, 2),
            FontSize = 11,
        };
        Grid.SetColumn(tb, col);
        row.Children.Add(tb);
    }

    

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();
}
