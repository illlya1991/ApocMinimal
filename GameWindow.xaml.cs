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
using System.Windows.Controls;
using ApocMinimal.Services;

namespace ApocMinimal;

public partial class GameWindow : Window
{
    private readonly GameViewModel _viewModel;
    private readonly DatabaseManager _db;
    private Border? _progressOverlay;
    private TextBlock? _progressText;
    private System.Windows.Controls.ProgressBar? _progressBar;

    public GameWindow(DatabaseManager db, GameInitState? state = null)
    {
        System.Diagnostics.Debug.WriteLine("=== GameWindow: НАЧАЛО КОНСТРУКТОРА ===");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            InitializeComponent();

            _db = db;
            db.OpenCurrentSave();

            _viewModel = new GameViewModel(db, LogPlayer, state);
            DataContext = _viewModel;
            _viewModel.PropertyChanged += (s, e) => RefreshHeader();

            PlayerActionsControl.SetViewModel(_viewModel);
            PlayerActionsControl.LogAction += LogPlayer;
            PlayerActionsControl.EndDayRequested += async () => await OnEndDay();
            PlayerActionsControl.QuestsRequested += OnQuestsRequested;
            PlayerActionsControl.PlayerInfoRequested += OnPlayerInfoRequested;
            PlayerActionsControl.FullscreenRequested += OnFullscreenRequested;
            PlayerActionsControl.SettingsRequested += OnSettingsRequested;
            NpcListControl.NpcSelected += OnNpcSelected;

            RefreshAll();
            Title = $"Apocalypse Simulation — {_viewModel.PlayerName}";

            sw.Stop();
            System.Diagnostics.Debug.WriteLine($"=== GameWindow: КОНСТРУКТОР ЗА {sw.ElapsedMilliseconds} мс ===");

            _ = LoadInitialDaysAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"!!! ОШИБКА В КОНСТРУКТОРЕ: {ex.Message}");
            throw;
        }
    }

    private async Task LoadInitialDaysAsync()
    {
        var totalSw = System.Diagnostics.Stopwatch.StartNew();
        System.Diagnostics.Debug.WriteLine($"=== LoadInitialDaysAsync: НАЧАЛО ===");

        try
        {
            if (_viewModel.CurrentDay == 1)
            {
                System.Diagnostics.Debug.WriteLine("  [1] Установка обменов дня 1...");
                _viewModel.SetupDayExchanges(1);

                LogControl.NewDay($"═══ ДЕНЬ 1 ══════════════════════");
                LogControl.AddSystemEntry("День 1 — мир создан.", "#8b949e");

                System.Diagnostics.Debug.WriteLine("  [2] Генерация дней 2-10...");

                for (int day = 2; day <= 10; day++)
                {
                    System.Diagnostics.Debug.WriteLine($"    Обработка дня {day - 1} -> переход к дню {day}");

                    // Обрабатываем конец предыдущего дня
                    var endResult = _viewModel.AdvanceToNextDay();
                    LogSystemSummary(endResult);

                    // Сохраняем лог предыдущего дня
                    SaveCurrentDayLog(day - 1);

                    // Создаём новый день
                    LogControl.NewDay($"═══ ДЕНЬ {day} ══════════════════════");

                    // Обрабатываем NPC для нового дня с прогрессом
                    // alertsOnly=true: не собираем 150к записей UI для авто-генерации
                    var progress = CreateProgressHandler();
                    StatusTextBlock.Text = $"Обработка дня {day}...";
                    StatusTextBlock.Visibility = Visibility.Visible;

                    var dayStartTime = DateTime.Now;
                    System.Diagnostics.Debug.WriteLine($"      Запуск ProcessNpcDayOptimizedAsync для дня {day}");

                    var npcResult = await _viewModel.ProcessNpcDayOptimizedAsync(progress, alertsOnly: true);

                    System.Diagnostics.Debug.WriteLine($"      ProcessNpcDayOptimizedAsync завершён за {(DateTime.Now - dayStartTime).TotalMilliseconds:F0} мс");

                    LogNpcDay(npcResult);

                    StatusTextBlock.Text = "";
                    StatusTextBlock.Visibility = Visibility.Collapsed;

                    LogControl.AddSystemEntry($"--- День {day} обработан ---", "#60a5fa");

                    // Даём UI время обновиться
                    await Task.Delay(10);

                    System.Diagnostics.Debug.WriteLine($"    День {day} полностью завершён");
                }

                System.Diagnostics.Debug.WriteLine("  [3] Применение обменов дня 10...");
                var day10Exchanges = _viewModel.SetupAndApplyDayExchanges(10);

                LogControl.NewDay($"═══ ДЕНЬ {_viewModel.CurrentDay} ══════════════════════");
                LogControl.AddSystemEntry("День 10 — первый день участия игрока.", "#8b949e");

                foreach (var ex in day10Exchanges)
                    LogControl.AddSystemEntry($"📜 Принят обмен: «{ex.Name}» — {ex.GetText}", "#f59e0b");

                LogControl.AddSystemEntry("👉 Игрок может начинать действовать!", "#56d364");
            }
            else
            {
                LoadSavedLogs();

                bool alreadyProcessed = _viewModel.AllNpcs
                    .Any(n => n.Memory.Any(m => m.Day == _viewModel.CurrentDay && m.Type == MemoryType.Action));

                LogControl.NewDay($"═══ ДЕНЬ {_viewModel.CurrentDay} ══════════════════════");

                if (!alreadyProcessed)
                {
                    LogControl.AddSystemEntry("Симуляция дня...", "#8b949e");
                    var dayResult = await _viewModel.ProcessNpcDayOptimizedAsync();
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

            totalSw.Stop();
            System.Diagnostics.Debug.WriteLine($"=== LoadInitialDaysAsync: ВСЕГО {totalSw.ElapsedMilliseconds} мс ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"!!! ОШИБКА В LoadInitialDaysAsync: {ex.Message}");
            LogControl.AddSystemEntry($"Ошибка: {ex.Message}", "#f87171");
        }
    }

    private Progress<(int current, int total)> CreateProgressHandler()
    {
        int lastPercent = 0;
        var lastTime = DateTime.Now;

        return new Progress<(int current, int total)>(p =>
        {
            if (p.total > 0)
            {
                int percent = (int)(p.current * 100.0 / p.total);
                var now = DateTime.Now;

                if (percent != lastPercent && (percent % 10 == 0 || p.current == p.total))
                {
                    var elapsed = (now - lastTime).TotalMilliseconds;
                    System.Diagnostics.Debug.WriteLine($"        Прогресс: {percent}% ({p.current}/{p.total}) - за {elapsed:F0} мс");
                    lastTime = now;
                    lastPercent = percent;
                }

                Dispatcher.Invoke(() =>
                {
                    StatusTextBlock.Text = $"День: {p.current}/{p.total} ({percent}%)";

                    if (p.current == p.total)
                    {
                        Task.Delay(1000).ContinueWith(_ =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                if (StatusTextBlock.Text.Contains("100%"))
                                {
                                    StatusTextBlock.Text = "";
                                    StatusTextBlock.Visibility = Visibility.Collapsed;
                                }
                            });
                        });
                    }
                });
            }
        });
    }

    private void SaveCurrentDayLog(int dayNumber)
    {
        var dayRaw = LogControl.GetDayRaw(dayNumber);
        if (dayRaw == null) return;

        Task.Run(() =>
        {
            _db.SaveDayLog(_db.CurrentSaveId, dayNumber,
                dayRaw.Entries.Select(e => (e.Section, e.Text, e.Color, e.IsAction)));
        });
    }

    private void LoadSavedLogs()
    {
        var allRows = _db.GetAllLogs(_db.CurrentSaveId);
        if (allRows.Count == 0) return;

        var rawDays = allRows
            .GroupBy(r => r.DayNumber)
            .OrderBy(g => g.Key)
            .Select(g => new ApocMinimal.Controls.LogDayData
            {
                DayNumber = g.Key,
                Entries = g.Select(r => new ApocMinimal.Controls.LogEntryData
                {
                    Section = r.Section,
                    Text = r.Text,
                    Color = r.Color,
                    IsAction = r.IsAction,
                }).ToList()
            })
            .ToList();

        LogControl.RebuildFromRaw(rawDays);
    }

    private void LogNpcDay(ApocMinimal.Systems.DayResult dayResult)
    {
        System.Diagnostics.Debug.WriteLine($"      LogNpcDay: начало, {dayResult.NpcResults.Count} результатов");

        foreach (var npcResult in dayResult.NpcResults)
        {
            var entries = npcResult.Actions;
            if (entries.Count == 0) continue;

            LogControl.BeginNpcSection(npcResult.Npc.Name);
            foreach (var entry in entries)
            {
                bool isAction = !entry.IsAlert;
                LogControl.AddNpcEntry($"  [{entry.Time}] {entry.Text}",
                    entry.IsAlert ? "#f87171" : entry.Color,
                    isAction);
            }
        }

        System.Diagnostics.Debug.WriteLine($"      LogNpcDay: завершено");
    }

    private void LogSystemSummary(ApocMinimal.Systems.DayResult dayResult)
    {
        foreach (var (text, isAlert) in dayResult.Logs)
            LogControl.AddSystemEntry(text, isAlert ? "#f87171" : "#8b949e");

        foreach (var (npc, q) in dayResult.QuestRewards)
        {
            var res = _viewModel.Resources.FirstOrDefault(r => r.Id == q.RewardResourceId);
            LogControl.AddSystemEntry(
                $"✓ Квест «{q.Title}» — {npc.Name} → +{q.RewardAmount:F0} {res?.Name}",
                "#22c55e");
        }

        LogControl.AddSystemEntry(
            $"23:00 | ОР: {_viewModel.DevPoints:F0}  Выживших: {_viewModel.AliveNpcsCount}/{_viewModel.AllNpcs.Count}  Терминал: ур.{_viewModel.TerminalLevel}",
            LogEntry.ColorTerminalColor);
    }

    private void LogPlayer(string text, string color)
    {
        LogControl.AddEntry(text, color);
    }

    private void OnNpcSelected(Npc npc)
    {
        NpcSidebarControl.Toggle(npc);
    }

    private void RefreshAll()
    {
        RefreshHeader();
        NpcListControl.UpdateNpcs(_viewModel.AllNpcs, _viewModel.ControlledZoneIds, NpcSidebarControl.CurrentNpc);
        PlayerActionsControl.Refresh();
    }

    private void RefreshHeader()
    {
        ListPlayerFactions listPlayerFactions = new ListPlayerFactions();
        OnePlayerFaction onePlayerFaction = listPlayerFactions.factions.FirstOrDefault(pf => pf.Faction == _viewModel.PlayerFaction);
        PlayerNameLabel.Text = _viewModel.PlayerName;
        PlayerFactionLabel.Text = $"  |  {onePlayerFaction.Label}";
        DayLabel.Text = $"  |  {_viewModel.DayDisplay}";
        DevPointsLabel.Text = $"  {_viewModel.DevPointsDisplay}";
        TerminalLabel.Text = $"  {_viewModel.TerminalDisplay}";
        BarrierLabel.Text = $"  Барьер: ур.{_viewModel.BarrierLevel}";
    }

    private async Task OnEndDay()
    {
        SettingsOverlay.Visibility = Visibility.Collapsed;

        var endResult = _viewModel.AdvanceToNextDay();

        LogControl.NewDay($"═══ ДЕНЬ {_viewModel.CurrentDay} ══════════════════════");
        LogSystemSummary(endResult);

        ShowProgress($"Обработка дня {_viewModel.CurrentDay}...");
        var progress = CreateProgressHandler();

        var npcResult = await _viewModel.ProcessNpcDayOptimizedAsync(progress);
        HideProgress();

        LogNpcDay(npcResult);

        _viewModel.SaveAll();

        var newExchanges = _viewModel.SetupAndApplyDayExchanges(_viewModel.CurrentDay);
        foreach (var ex in newExchanges)
            LogControl.AddSystemEntry($"📜 Принят обмен: «{ex.Name}» — {ex.GetText}", "#f59e0b");

        _viewModel.Refresh();
        RefreshAll();

        SaveCurrentDayLog(_viewModel.CurrentDay - 1);

        if (_viewModel.IsVictory || _viewModel.IsDefeat)
            ShowResultWindow();
    }

    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SaveAll();
        StatusTextBlock.Text = "Сохранено";
        StatusTextBlock.Visibility = Visibility.Visible;
        Task.Delay(2000).ContinueWith(_ =>
            Dispatcher.Invoke(() =>
            {
                StatusTextBlock.Text = "";
                StatusTextBlock.Visibility = Visibility.Collapsed;
            }));
    }

    private void ExitBtn_Click(object sender, RoutedEventArgs e)
    {
        new StartWindow().Show();
        Close();
    }

    private void OnQuestsRequested()
    {
        var w = new QuestWindow(_viewModel, LogPlayer);
        w.ShowDialog();
        _viewModel.Refresh();
        RefreshAll();
    }

    private void OnPlayerInfoRequested()
    {
        var w = new PlayerInfoWindow(_viewModel) { Owner = this };
        w.ShowDialog();
    }

    private void OnFullscreenRequested()
    {
        var w = new NpcFullscreenWindow(_viewModel.AllNpcs);
        w.ShowDialog();
    }

    private void OnSettingsRequested()
    {
        SettingsOverlay.Visibility = SettingsOverlay.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void ShowProgress(string message)
    {
        if (_progressOverlay != null) return;

        _progressOverlay = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(220, 13, 17, 23)),
            CornerRadius = new CornerRadius(8),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60a5fa")),
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 300,
            Height = 80,
        };

        var stack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(16)
        };

        var msgText = new TextBlock
        {
            Text = message,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c9d1d9")),
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 8),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        stack.Children.Add(msgText);

        _progressBar = new System.Windows.Controls.ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Height = 6,
            Width = 200,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60a5fa")),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#21262d"))
        };
        stack.Children.Add(_progressBar);

        _progressText = new TextBlock
        {
            Text = "0%",
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b949e")),
            FontSize = 10,
            Margin = new Thickness(0, 4, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        stack.Children.Add(_progressText);

        _progressOverlay.Child = stack;
        var mainGrid = (Grid)Content;
        mainGrid.Children.Add(_progressOverlay);
        Canvas.SetZIndex(_progressOverlay, 1000);
    }

    private void HideProgress()
    {
        if (_progressOverlay != null)
        {
            var mainGrid = (Grid)Content;
            if (mainGrid.Children.Contains(_progressOverlay))
                mainGrid.Children.Remove(_progressOverlay);
            _progressOverlay = null;
            _progressBar = null;
            _progressText = null;
        }
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