using System.Windows;
using ApocMinimal.Database;

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

            var loading = new LoadingWindow(
                "Создание нового мира...",
                (worker, args) =>
                {
                    _db.ResetDatabase((percent, status, detail) =>
                    {
                        worker.ReportProgress(percent, (status, detail));
                    });
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
