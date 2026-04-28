using System.Windows;
using System.Windows.Controls;
using ApocMinimal.Database;
using ApocMinimal.Models.PersonData;

namespace ApocMinimal;

public partial class StartWindow : Window
{
    private readonly DatabaseManager _db = new();

    public StartWindow()
    {
        InitializeComponent();
        PopulateFactionCombo();
        CheckSave();
    }

    private void PopulateFactionCombo()
    {
        var factions = new[]
        {
            PlayerFaction.ElementMages,
            PlayerFaction.PathBlades,
            PlayerFaction.MirrorHealers,
            PlayerFaction.DeepSmiths,
            PlayerFaction.GuardHeralds,
        };
        foreach (var f in factions)
            FactionCombo.Items.Add(f.ToLabel());
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
    }

    private PlayerFaction SelectedFaction()
    {
        return FactionCombo.SelectedIndex switch
        {
            1 => PlayerFaction.PathBlades,
            2 => PlayerFaction.MirrorHealers,
            3 => PlayerFaction.DeepSmiths,
            4 => PlayerFaction.GuardHeralds,
            _ => PlayerFaction.ElementMages,
        };
    }

    private void CheckSave()
    {
        if (_db.HasAnyActiveSave())
        {
            ContinueButton.IsEnabled = true;
        }
        else
        {
            ContinueButton.IsEnabled = false;
            ContinueButton.Background = System.Windows.Media.Brushes.Gray;
        }
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
