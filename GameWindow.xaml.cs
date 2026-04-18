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

        _viewModel = new GameViewModel(db, Log);
        DataContext = _viewModel;
        _viewModel.PropertyChanged += (s, e) => RefreshHeader();

        // Инициализация контролов
        PlayerActionsControl.SetViewModel(_viewModel);
        PlayerActionsControl.LogAction += Log;
        NpcListControl.NpcSelected += OnNpcSelected;

        RefreshAll();

        Title = $"Apocalypse Simulation — {_viewModel.PlayerName}";

        // Show NPC actions for the current day if not yet shown
        bool alreadyProcessed = _viewModel.AllNpcs
            .Any(n => n.Memory.Any(m => m.Day == _viewModel.CurrentDay && m.Type == MemoryType.Action));
        LogDay($"═══ ДЕНЬ {_viewModel.CurrentDay} ══════════════════════");
        Log($"Мир загружен. Выживших: {_viewModel.AliveNpcsCount}", LogEntry.ColorNormal);
        if (!alreadyProcessed)
            _viewModel.ProcessNpcDay(Log);
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
        DayLabel.Text = $"  |  {_viewModel.DayDisplay}";
        FaithLabel.Text = $"  {_viewModel.FaithDisplay}";
        AltarLabel.Text = $"  {_viewModel.AltarDisplay}";
        ActionsLabel.Text = $"  {_viewModel.ActionsDisplay}";
        ActionsLabel.Foreground = _viewModel.HasActionsLeft ?
            (SolidColorBrush)new BrushConverter().ConvertFromString("#56d364")! :
            (SolidColorBrush)new BrushConverter().ConvertFromString("#f87171")!;
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

        // Завершить текущий день (квесты, нужды, вера) и перейти к следующему
        _viewModel.AdvanceToNextDay(Log);
        _viewModel.SaveAll();

        Log($"ОВ: {_viewModel.FaithPoints:F0}  |  Выживших: {_viewModel.AliveNpcsCount}/{_viewModel.AllNpcs.Count}", LogEntry.ColorAltarColor);

        // Начало нового дня — действия НПС
        LogDay($"═══ ДЕНЬ {_viewModel.CurrentDay} ══════════════════════");
        _viewModel.ProcessNpcDay(Log);

        _viewModel.Refresh();
        RefreshAll();
    }

    // =========================================================
    // Logging
    // =========================================================

    private void LogDay(string header)
    {
        LogControl.AddEntry(header, LogEntry.ColorDay);
    }

    private void Log(string text, string color)
    {
        LogControl.AddEntry(text, color);
    }

    // =========================================================
    // Fullscreen Window
    // =========================================================

    private void FullscreenInfo_Click(object sender, RoutedEventArgs e)
    {
        var fullscreenWindow = new NpcFullscreenWindow(_viewModel.AllNpcs);
        fullscreenWindow.ShowDialog();
    }

    private void QuestsBtn_Click(object sender, RoutedEventArgs e)
    {
        var questWindow = new QuestWindow(_viewModel, Log);
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