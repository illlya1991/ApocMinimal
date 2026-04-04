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
        if (_db.SaveExists)
        {
            ContinueButton.IsEnabled = true;
            var p = _db.GetPlayer();
            SaveInfoText.Text = p != null
                ? $"Сохранение: День {p.CurrentDay} | Алтарь ур.{p.AltarLevel} | ОВ: {p.FaithPoints:F0}"
                : "Сохранение найдено";
        }
        else
        {
            ContinueButton.IsEnabled = false;
            ContinueButton.Background = System.Windows.Media.Brushes.Gray;
            SaveInfoText.Text = "Сохранений нет";
        }
    }

    private void NewGame_Click(object sender, RoutedEventArgs e)
    {
        _db.ResetDatabase();
        OpenGame(_db);
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        OpenGame(_db);
    }

    private void OpenGame(DatabaseManager db)
    {
        var game = new GameWindow(db);
        game.Show();
        Close();
    }
}
