using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ApocMinimal.Controls;

public partial class ToastNotification : UserControl
{
    private DispatcherTimer _autoCloseTimer;

    public ToastNotification(string message, string type = "info")
    {
        InitializeComponent();
        MessageText.Text = message;

        switch (type)
        {
            case "success":
                IconText.Text = "✓";
                ((Border)Parent).BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ade80"));
                break;
            case "warning":
                IconText.Text = "⚠";
                ((Border)Parent).BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fbbf24"));
                break;
            case "error":
                IconText.Text = "✗";
                ((Border)Parent).BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f87171"));
                break;
            default:
                IconText.Text = "ℹ";
                ((Border)Parent).BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60a5fa"));
                break;
        }

        _autoCloseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _autoCloseTimer.Tick += (s, e) => Close();
        _autoCloseTimer.Start();
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

    private void Close()
    {
        _autoCloseTimer.Stop();
        if (Parent is Panel panel)
            panel.Children.Remove(this);
    }
}