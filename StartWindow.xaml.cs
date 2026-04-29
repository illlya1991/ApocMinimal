using System.Windows;
using System.Windows.Controls;
using ApocMinimal.Database;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Services;

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

        var state = new GameInitState();

        var loading = new LoadingWindow(
            "Создание нового мира...",
            (worker, args) =>
            {
                // ResetDatabase: 0–70%
                _db.ResetDatabase((percent, status, detail) =>
                {
                    worker.ReportProgress(percent * 70 / 100,
                        string.IsNullOrEmpty(detail) ? (object)status : (status, detail));
                });

                // Player setup
                worker.ReportProgress(71, "Настройка персонажа...");
                var player = _db.GetPlayer();
                if (player != null)
                {
                    player.Name   = chosenName;
                    player.Faction = chosenFaction;
                    _db.EnsureFactionCoeffsInGameConfig();
                    _db.ApplyFactionCoefficients(player);
                    _db.SavePlayer(player);
                }

                // LocationService: 75–88%
                worker.ReportProgress(75, "Инициализация карты...");
                state.LocationService = new LocationService(_db);
                state.LocationService.Initialize();

                // TechniqueService: 88–100%
                worker.ReportProgress(88,
                    ("Загрузка техник...", $"{state.LocationService.TotalLocations} локаций загружено"));
                state.TechniqueService = new TechniqueService(_db);
                state.TechniqueService.Initialize();

                worker.ReportProgress(100,
                    ("Готово!", $"{state.TechniqueService.TotalTechniques} техник загружено"));
            },
            () => OpenGame(_db, state));

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

            var state = new GameInitState();

            var loading = new LoadingWindow(
                "Загрузка сохранения...",
                (worker, args) =>
                {
                    // Open DB
                    worker.ReportProgress(5, "Открытие базы данных...");
                    _db.OpenCurrentSave();

                    // LocationService: 10–65%
                    worker.ReportProgress(10, "Загрузка карты...");
                    state.LocationService = new LocationService(_db);
                    state.LocationService.Initialize();

                    // TechniqueService: 65–95%
                    worker.ReportProgress(65,
                        ("Загрузка техник...", $"{state.LocationService.TotalLocations} локаций загружено"));
                    state.TechniqueService = new TechniqueService(_db);
                    state.TechniqueService.Initialize();

                    worker.ReportProgress(100,
                        ("Готово!", $"{state.TechniqueService.TotalTechniques} техник загружено"));
                },
                () => OpenGame(_db, state));

            loading.Owner = this;
            loading.ShowDialog();
        }

        CheckSave();
    }

    private void OpenGame(DatabaseManager db, GameInitState? state = null)
    {
        var game = new GameWindow(db, state);
        game.Show();
        Close();
    }
}
