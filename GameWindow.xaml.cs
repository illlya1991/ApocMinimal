using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ApocMinimal.Database;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.UIData;
using ApocMinimal.Systems;
using ApocMinimal.ViewModels;
using ApocMinimal.Controls;

namespace ApocMinimal;

public partial class GameWindow : Window
{
    private readonly GameViewModel _viewModel;

    public GameWindow(DatabaseManager db)
    {
        InitializeComponent();

        db.OpenCurrentSave();
        db.EnsureNpcModifiersSchema();
        db.EnsurePlayerSchema();
        db.EnsureNpcTechSchema();
        _viewModel = new GameViewModel(db, LogPlayer);
        DataContext = _viewModel;
        _viewModel.PropertyChanged += (s, e) => RefreshHeader();

        PlayerActionsControl.SetViewModel(_viewModel);
        PlayerActionsControl.LogAction += LogPlayer;
        NpcListControl.NpcSelected += OnNpcSelected;

        RefreshAll();
        Title = $"Apocalypse Simulation — {_viewModel.PlayerName}";

        if (_viewModel.CurrentDay == 1)
        {
            _viewModel.SetupDayExchanges(1);

            for (int d = 1; d <= 9; d++)
            {
                LogControl.NewDay($"═══ ДЕНЬ {d} ══════════════════════");
                var npcRes = _viewModel.ProcessNpcDay();
                LogNpcDay(npcRes);
                var endRes = _viewModel.AdvanceToNextDay();
                LogSystemSummary(endRes);
                _viewModel.SaveAll();
            }
            var day10Exchanges = _viewModel.SetupAndApplyDayExchanges(10);

            LogControl.NewDay($"═══ ДЕНЬ {_viewModel.CurrentDay} ══════════════════════");
            LogControl.AddSystemEntry("День 10 — первый день участия игрока.", "#8b949e");

            foreach (var ex in day10Exchanges)
                LogControl.AddSystemEntry($"📜 Принят обмен: «{ex.Name}» — {ex.GetText}", "#f59e0b");

            var day10Result = _viewModel.ProcessNpcDay();
            LogNpcDay(day10Result);
            LogSystemSummary(day10Result);

            _viewModel.SaveAll();
        }
        else
        {
            bool alreadyProcessed = _viewModel.AllNpcs
                .Any(n => n.Memory.Any(m => m.Day == _viewModel.CurrentDay && m.Type == MemoryType.Action));

            LogControl.NewDay($"═══ ДЕНЬ {_viewModel.CurrentDay} ══════════════════════");

            if (!alreadyProcessed)
            {
                var dayResult = _viewModel.ProcessNpcDay();
                LogNpcDay(dayResult);
                LogSystemSummary(dayResult);
            }
            else
            {
                LogControl.AddSystemEntry($"Мир загружен. Выживших: {_viewModel.AliveNpcsCount}", LogEntry.ColorNormal);
            }

            var todayExchanges = _viewModel.SetupAndApplyDayExchanges(_viewModel.CurrentDay);
            foreach (var ex in todayExchanges)
                LogControl.AddSystemEntry($"📜 Принят обмен: «{ex.Name}» — {ex.GetText}", "#f59e0b");
        }

        PlayerActionsControl.Refresh();
    }

    private void OnNpcSelected(Npc npc)
    {
        NpcSidebarControl.Toggle(npc);
    }

    private void RefreshAll()
    {
        RefreshHeader();
        NpcListControl.UpdateNpcs(_viewModel.AllNpcs, NpcSidebarControl.CurrentNpc);
        PlayerActionsControl.Refresh();
    }

    private void RefreshHeader()
    {
        ListPlayerFactions listPlayerFactions = new ListPlayerFactions();
        OnePlayerFaction onePlayerFaction = listPlayerFactions.factions.FirstOrDefault(pf => pf.Faction == _viewModel.PlayerFaction);
        PlayerNameLabel.Text    = $"{_viewModel.PlayerName}";
        PlayerFactionLabel.Text = $"  |  {onePlayerFaction.Label}";
        DayLabel.Text           = $"  |  {_viewModel.DayDisplay}";
        FaithLabel.Text         = $"  {_viewModel.DevPointsDisplay}";
        AltarLabel.Text         = $"  {_viewModel.TerminalDisplay}";
        ActionsLabel.Text       = $"  {_viewModel.ActionsDisplay}";
        ActionsLabel.Foreground = _viewModel.HasActionsLeft
            ? (SolidColorBrush)new BrushConverter().ConvertFromString("#56d364")!
            : (SolidColorBrush)new BrushConverter().ConvertFromString("#f87171")!;
    }

    // =========================================================
    // End of Day
    // =========================================================

    private void EndDayBtn_Click(object sender, RoutedEventArgs e)
    {
        SettingsOverlay.Visibility = Visibility.Collapsed;

        var scaleAnim = new DoubleAnimation
        {
            From = 1, To = 0.95,
            Duration = TimeSpan.FromMilliseconds(50),
            AutoReverse = true
        };
        EndDayBtn.RenderTransform = new ScaleTransform();
        EndDayBtn.RenderTransformOrigin = new Point(0.5, 0.5);
        EndDayBtn.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
        EndDayBtn.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

        // End current day: quests, needs, faith
        var endResult = _viewModel.AdvanceToNextDay();
        _viewModel.SaveAll();

        // Start new day header first — system summary goes into the NEW day's Система section
        LogControl.NewDay($"═══ ДЕНЬ {_viewModel.CurrentDay} ══════════════════════");
        LogSystemSummary(endResult);
        LogNpcDay(_viewModel.ProcessNpcDay());

        var newExchanges = _viewModel.SetupAndApplyDayExchanges(_viewModel.CurrentDay);
        foreach (var ex in newExchanges)
            LogControl.AddSystemEntry($"📜 Принят обмен: «{ex.Name}» — {ex.GetText}", "#f59e0b");

        _viewModel.Refresh();
        RefreshAll();

        if (_viewModel.IsVictory || _viewModel.IsDefeat)
            ShowResultWindow();
    }

    private void ShowResultWindow()
    {
        int aliveFollowers = _viewModel.AliveNpcs.Count(n => n.FollowerLevel > 0);
        string summary = _viewModel.TrueTerminal.GetProgressSummary(
            _viewModel.TerminalLevel,
            aliveFollowers,
            _viewModel.DevPoints,
            _viewModel.CurrentDay);
        var win = new ResultWindow(_viewModel.IsVictory, _viewModel.CurrentDay, summary);
        win.Owner = this;
        win.ShowDialog();
    }

    // =========================================================
    // Log routing helpers
    // =========================================================

    /// <summary>Log all NPC results into the NPC section.</summary>
    private void LogNpcDay(ApocMinimal.Systems.DayResult dayResult)
    {
        foreach (var npcResult in dayResult.NpcResults)
        {
            LogControl.BeginNpcSection(npcResult.Npc.Name);
            foreach (var entry in npcResult.Actions)
            {
                bool isAction = !entry.IsAlert;
                LogControl.AddNpcEntry($"  [{entry.Time}] {entry.Text}",
                    entry.IsAlert ? "#f87171" : entry.Color,
                    isAction);
            }
        }
    }

    /// <summary>Log end-of-day system info (resources, faith, followers, quests).</summary>
    private void LogSystemSummary(ApocMinimal.Systems.DayResult dayResult)
    {
        // System logs (resources consumed, leader bonus, injuries)
        foreach (var (text, isAlert) in dayResult.Logs)
            LogControl.AddSystemEntry(text, isAlert ? "#f87171" : "#8b949e");

        // Quest completions
        foreach (var (npc, q) in dayResult.QuestRewards)
        {
            var res = _viewModel.Resources.FirstOrDefault(r => r.Id == q.RewardResourceId);
            LogControl.AddSystemEntry(
                $"✓ Квест «{q.Title}» — {npc.Name} → +{q.RewardAmount:F0} {res?.Name}",
                "#22c55e");
        }

        // Faith & survivors summary
        LogControl.AddSystemEntry(
            $"23:00 | ОР: {_viewModel.DevPoints:F0}  Выживших: {_viewModel.AliveNpcsCount}/{_viewModel.AllNpcs.Count}  Терминал: ур.{_viewModel.TerminalLevel}",
            LogEntry.ColorAltarColor);

        // Follower table
        var sb = new System.Text.StringBuilder("Последователи: ");
        for (int fl = 1; fl <= 5; fl++)
        {
            int lim = _viewModel.GetFollowerLimit(fl);
            if (lim == 0) continue;
            int cur = _viewModel.GetFollowerCountAtLevel(fl);
            sb.Append($"[ур.{fl}: {cur}/{(lim == -1 ? "∞" : lim.ToString())}]  ");
        }
        LogControl.AddSystemEntry(sb.ToString().TrimEnd(), "#60a5fa");
    }

    /// <summary>Route player action log lines to the "Действия игрока" section.</summary>
    private void LogPlayer(string text, string color)
    {
        LogControl.AddEntry(text, color);
    }

    // =========================================================
    // Other windows
    // =========================================================

    private void FullscreenInfo_Click(object sender, RoutedEventArgs e)
    {
        var fullscreenWindow = new NpcFullscreenWindow(_viewModel.AllNpcs);
        fullscreenWindow.ShowDialog();
    }

    private void QuestsBtn_Click(object sender, RoutedEventArgs e)
    {
        var questWindow = new QuestWindow(_viewModel, LogPlayer);
        questWindow.ShowDialog();
        _viewModel.Refresh();
        RefreshAll();
    }

    // =========================================================
    // Settings
    // =========================================================

    private void SettingsBtn_Click(object sender, RoutedEventArgs e)
    {
        SettingsOverlay.Visibility = SettingsOverlay.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void MenuNoSave_Click(object sender, RoutedEventArgs e)
    {
        SettingsOverlay.Visibility = Visibility.Collapsed;
        new StartWindow().Show();
        Close();
    }

    private void MenuSave_Click(object sender, RoutedEventArgs e)
    {
        SettingsOverlay.Visibility = Visibility.Collapsed;
        _viewModel.SaveAll();
        new StartWindow().Show();
        Close();
    }

    private void ExitGame_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
