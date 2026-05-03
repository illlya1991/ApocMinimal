// StartWindow.xaml.cs (исправленная версия)
using System.Windows;
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

    private async void NewGame_Click(object sender, RoutedEventArgs e)
    {
        var saveWindow = new SaveSelectionWindow(_db.ListSaves, "new", _db);
        bool? saveResult = saveWindow.ShowDialog();
        if (saveResult != true || saveWindow.IsCanceled || saveWindow.SelectedSave == null)
            return;

        // Очищаем кэши перед новой игрой
        DatabaseManager.ClearAllCaches();
        _db.ResetForNewSave();

        _db.ThisSave = saveWindow.SelectedSave;

        var setupWindow = new NewGameSetupWindow();
        setupWindow.Owner = this;
        bool? setupResult = setupWindow.ShowDialog();
        if (setupResult != true || !setupWindow.Confirmed)
            return;

        string chosenName = setupWindow.ChosenName;
        PlayerFaction chosenFaction = setupWindow.ChosenFaction;

        var state = new GameInitState();
        state.LocationService = new LocationService(_db);
        state.TechniqueService = new TechniqueService(_db);

        var loading = new LoadingWindow(
            "Создание нового мира...",
            (worker, args) =>
            {
                // Сброс БД
                _db.ResetDatabase((percent, status, detail) =>
                {
                    worker.ReportProgress(percent,
                        string.IsNullOrEmpty(detail) ? (object)status : (status, detail));
                });

                // Настройка игрока
                worker.ReportProgress(70, "Настройка персонажа...");
                var player = _db.GetPlayer();
                if (player != null)
                {
                    player.Name = chosenName;
                    player.Faction = chosenFaction;
                    _db.ApplyFactionCoefficients(player);
                    _db.SavePlayer(player);
                }

                // Синхронная загрузка локаций (но с прогрессом через worker)
                worker.ReportProgress(75, "Загрузка карты...");
                state.LocationService.Initialize();

                // Синхронная загрузка техник
                worker.ReportProgress(88,
                    ("Загрузка техник...", $"{state.LocationService.TotalLocations} локаций загружено"));
                state.TechniqueService.Initialize();

                worker.ReportProgress(100,
                    ("Готово!", $"{state.TechniqueService.TotalTechniques} техник загружено"));
            },
            () => OpenGame(_db, state));

        loading.Owner = this;
        loading.ShowDialog();
    }

    private async void Continue_Click(object sender, RoutedEventArgs e)
    {
        var activeSaves = _db.ListSaves.FindAll(save => save._active);
        var saveWindow = new SaveSelectionWindow(activeSaves, "continue", _db);
        bool? result = saveWindow.ShowDialog();

        if (result == true && !saveWindow.IsCanceled && saveWindow.SelectedSave != null)
        {
            // Очищаем кэши перед новой игрой
            DatabaseManager.ClearAllCaches();
            _db.ResetForNewSave();

            _db.ThisSave = saveWindow.SelectedSave;

            var state = new GameInitState();
            state.LocationService = new LocationService(_db);
            state.TechniqueService = new TechniqueService(_db);

            var loading = new LoadingWindow(
                "Загрузка сохранения...",
                (worker, args) =>
                {
                    // Открываем БД
                    worker.ReportProgress(5, "Открытие базы данных...");
                    _db.OpenCurrentSave();

                    // Загрузка локаций
                    worker.ReportProgress(10, "Загрузка карты...");
                    state.LocationService.Initialize();

                    // Загрузка техник
                    worker.ReportProgress(65,
                        ("Загрузка техник...", $"{state.LocationService.TotalLocations} локаций загружено"));
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