using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ApocMinimal.Database;
using ApocMinimal.Models.PersonData;

namespace ApocMinimal;

public partial class StartWindow : Window
{
    private readonly DatabaseManager _db = new();

    private static readonly (PlayerFaction Faction, string Color)[] FactionColors = new[]
    {
        (PlayerFaction.ElementMages,  "#79c0ff"),
        (PlayerFaction.PathBlades,    "#f87171"),
        (PlayerFaction.MirrorHealers, "#56d364"),
        (PlayerFaction.DeepSmiths,    "#e3b341"),
        (PlayerFaction.GuardHeralds,  "#d2a8ff"),
    };

    public StartWindow()
    {
        InitializeComponent();
        PopulateFactionCombo();
        CheckSave();
    }

    private void PopulateFactionCombo()
    {
        foreach (var (faction, color) in FactionColors)
        {
            var item = new ComboBoxItem
            {
                Content = faction.ToLabel(),
                Foreground = (Brush)new BrushConverter().ConvertFromString(color)!,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Color.FromRgb(0x16, 0x1b, 0x22)),
                Tag = faction,
            };
            FactionCombo.Items.Add(item);
        }
        FactionCombo.SelectedIndex = 0;
        FactionCombo.SelectionChanged += FactionCombo_SelectionChanged;
        UpdateFactionDesc();
    }

    private void FactionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => UpdateFactionDesc();

    private void UpdateFactionDesc()
    {
        var faction = SelectedFaction();
        FactionDescText.Text = faction.ToDescription();
        var entry = System.Array.Find(FactionColors, x => x.Faction == faction);
        if (entry != default)
            FactionDescText.Foreground = (Brush)new BrushConverter().ConvertFromString(entry.Color)!;
    }

    private PlayerFaction SelectedFaction()
    {
        if (FactionCombo.SelectedItem is ComboBoxItem item && item.Tag is PlayerFaction f)
            return f;
        return PlayerFaction.ElementMages;
    }

    private void CheckSave()
    {
        bool hasSave = _db.HasAnyActiveSave();
        ContinueButton.IsEnabled = hasSave;
        if (!hasSave)
            ContinueButton.Background = System.Windows.Media.Brushes.Gray;

        // Faction selection is only relevant for new games
        FactionSection.Visibility = Visibility.Visible;
    }

    private void NewGame_Click(object sender, RoutedEventArgs e)
    {
        var saveWindow = new SaveSelectionWindow(_db.ListSaves, "new", _db);
        bool? result = saveWindow.ShowDialog();

        if (result == true && !saveWindow.IsCanceled && saveWindow.SelectedSave != null)
        {
            _db.ThisSave = saveWindow.SelectedSave;
            var chosenFaction = SelectedFaction();

            var loading = new LoadingWindow(
                "Создание нового мира...",
                (worker, args) =>
                {
                    _db.ResetDatabase((percent, status, detail) =>
                    {
                        worker.ReportProgress(percent, (status, detail));
                    });
                    // Apply faction to player after world creation
                    var player = _db.GetPlayer();
                    if (player != null)
                    {
                        player.Faction = chosenFaction;
                        _db.SavePlayer(player);
                    }
                },
                () =>
                {
                    OpenGame(_db);
                });

            loading.Owner = this;
            loading.ShowDialog();
        }
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        var activeSaves = _db.ListSaves.FindAll(save => save._active);
        var saveWindow = new SaveSelectionWindow(activeSaves, "continue", _db);
        bool? result = saveWindow.ShowDialog();

        if (result == true && !saveWindow.IsCanceled && saveWindow.SelectedSave != null)
        {
            _db.ThisSave = saveWindow.SelectedSave;

            var loading = new LoadingWindow(
                "Загрузка сохранения...",
                (worker, args) =>
                {
                    worker.ReportProgress(50, "Загрузка данных...");
                    // Загрузка происходит в GameWindow
                },
                () =>
                {
                    OpenGame(_db);
                });

            loading.Owner = this;
            loading.ShowDialog();
        }
        CheckSave();
    }

    private void OpenGame(DatabaseManager db)
    {
        var game = new GameWindow(db);
        game.Show();
        Close();
    }
}
