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
        CheckSave();
    }

    private void CheckSave()
    {
        bool hasSave = _db.HasAnyActiveSave();
        ContinueButton.IsEnabled = hasSave;
        if (!hasSave)
            ContinueButton.Background = System.Windows.Media.Brushes.Gray;
    }

    private void NewGame_Click(object sender, RoutedEventArgs e)
    {
        var saveWindow = new SaveSelectionWindow(_db.ListSaves, "new", _db);
        bool? saveResult = saveWindow.ShowDialog();
        if (saveResult != true || saveWindow.IsCanceled || saveWindow.SelectedSave == null)
            return;

        _db.ThisSave = saveWindow.SelectedSave;

        var setupWindow = new NewGameSetupWindow();
        setupWindow.Owner = this;
        bool? setupResult = setupWindow.ShowDialog();
        if (setupResult != true || !setupWindow.Confirmed)
            return;

        string chosenName = setupWindow.ChosenName;
        PlayerFaction chosenFaction = setupWindow.ChosenFaction;

        var loading = new LoadingWindow(
            "Создание нового мира...",
            (worker, args) =>
            {
                _db.ResetDatabase((percent, status, detail) =>
                {
                    worker.ReportProgress(percent, (status, detail));
                });
                var player = _db.GetPlayer();
                if (player != null)
                {
                    player.Name = chosenName;
                    player.Faction = chosenFaction;
                    _db.EnsureFactionCoeffsInGameConfig();
                    _db.ApplyFactionCoefficients(player);
                    _db.SavePlayer(player);
                }
            },
            () => OpenGame(_db));

        loading.Owner = this;
        loading.ShowDialog();
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
