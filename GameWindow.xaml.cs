using System.Windows;
using System.Windows.Media;
using ApocMinimal.Database;
using ApocMinimal.Models.PersonData;
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
        PlayerActionsControl = new PlayerActionsControl();
        PlayerActionsControl.SetViewModel(_viewModel);
        PlayerActionsControl.LogAction += Log;
        NpcListControl.NpcSelected += OnNpcSelected;

        RefreshAll();

        Title = $"Apocalypse Simulation — {_viewModel.PlayerName}";
        LogDay($"=== День {_viewModel.CurrentDay} ===");
        Log($"Мир загружен. Выживших: {_viewModel.AliveNpcsCount}", LogEntry.ColorNormal);
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
        _viewModel.ProcessEndOfDay(Log);
        _viewModel.SaveAll();

        LogDay($"═══ ДЕНЬ {_viewModel.CurrentDay} ══════════════════════");

        Log($"Получено ОВ: (последователей: {_viewModel.AliveNpcsCount})", LogEntry.ColorAltarColor);
        Log($"Выживших: {_viewModel.AliveNpcsCount}/{_viewModel.AllNpcs.Count}  |  Вера: {_viewModel.FaithPoints:F0}", LogEntry.ColorDay);

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
}