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
            //var p = _db.GetPlayer();
            //SaveInfoText.Text = p != null
            //    ? $"Сохранение: День {p.CurrentDay} | Алтарь ур.{p.AltarLevel} | ОВ: {p.FaithPoints:F0}"
            //    : "Сохранение найдено";
        }
        else
        {
            ContinueButton.IsEnabled = false;
            ContinueButton.Background = System.Windows.Media.Brushes.Gray;
            //SaveInfoText.Text = "Сохранений нет";
        }
    }

    private void NewGame_Click(object sender, RoutedEventArgs e)
    {
        // Відкриваємо вікно вибору для нової гри
        var saveWindow = new SaveSelectionWindow(_db.ListSaves, "new", _db);
        bool? result = saveWindow.ShowDialog();

        if (result == true && !saveWindow.IsCanceled && saveWindow.SelectedSave != null)
        {
            // Починаємо нову гру з вибраним збереженням
            _db.ThisSave = saveWindow.SelectedSave;
            _db.ResetDatabase();
            OpenGame(_db);
        }
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        // Відкриваємо вікно вибору для збереженої гри
        var activeSaves = _db.ListSaves.FindAll(save => save._active);
        var saveWindow = new SaveSelectionWindow(activeSaves, "continue", _db);
        bool? result = saveWindow.ShowDialog();

        if (result == true && !saveWindow.IsCanceled && saveWindow.SelectedSave != null)
        {
            // Відновлюємо гру з вибраним збереженням
            _db.ThisSave = saveWindow.SelectedSave;
            OpenGame(_db);
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
