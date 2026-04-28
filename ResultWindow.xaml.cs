using System.Windows;
using System.Windows.Media;

namespace ApocMinimal;

public partial class ResultWindow : Window
{
    public ResultWindow(bool isVictory, int day, string progressSummary)
    {
        InitializeComponent();

        if (isVictory)
        {
            TitleText.Text = "ПОБЕДА";
            TitleText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#56d364"));
            SubtitleText.Text = $"Истинный Терминал достигнут на день {day}. Ты выжил.";
        }
        else
        {
            TitleText.Text = "ПОРАЖЕНИЕ";
            TitleText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f87171"));
            SubtitleText.Text = $"День {day}. Время вышло — мир поглотила тьма.";
        }

        ProgressText.Text = progressSummary;
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        // Close all windows and return to start
        foreach (Window w in Application.Current.Windows)
            if (w != this) w.Close();
        Close();

        var start = new StartWindow();
        start.Show();
    }
}
