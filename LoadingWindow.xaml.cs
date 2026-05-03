using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace ApocMinimal;

public partial class LoadingWindow : Window
{
    private readonly BackgroundWorker _worker;
    private readonly Action<BackgroundWorker, DoWorkEventArgs> _workAction;
    private readonly Action _onCompleted;
    private bool _isFinalizing = false;

    public LoadingWindow(string title, Action<BackgroundWorker, DoWorkEventArgs> workAction, Action onCompleted)
    {
        InitializeComponent();

        TitleText.Text = title;
        _workAction = workAction;
        _onCompleted = onCompleted;

        _worker = new BackgroundWorker
        {
            WorkerReportsProgress = true,
            WorkerSupportsCancellation = false
        };

        _worker.DoWork += Worker_DoWork;
        _worker.ProgressChanged += Worker_ProgressChanged;
        _worker.RunWorkerCompleted += Worker_RunWorkerCompleted;

        Loaded += (s, e) => _worker.RunWorkerAsync();
    }

    private void Worker_DoWork(object sender, DoWorkEventArgs e)
    {
        _workAction(_worker, e);
    }

    private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        int pct = e.ProgressPercentage;
        if (pct >= 0 && !_isFinalizing)
        {
            Dispatcher.Invoke(() =>
            {
                LoadingProgress.IsIndeterminate = false;
                LoadingProgress.Value = pct;
                PercentText.Text = $"{pct}%";
            });
        }

        if (e.UserState is string message)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = message;
                DetailText.Text = "";
            });
        }
        else if (e.UserState is ValueTuple<string, string> tuple)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = tuple.Item1;
                DetailText.Text = tuple.Item2;
            });
        }

        // Если достигли 100% и не в финальной стадии - переключаемся
        if (pct >= 100 && !_isFinalizing)
        {
            SetFinalizingStage();
        }
    }

    public void SetFinalizingStage()
    {
        _isFinalizing = true;

        Dispatcher.Invoke(() =>
        {
            LoadingProgress.IsIndeterminate = false;
            LoadingProgress.Value = 100;
            PercentText.Text = "100%";
            StatusText.Text = "Логика загружена";
            DetailText.Text = "Настраивается графика...";
            TitleText.Text = "Загрузка завершена";

            // Добавляем анимацию пульсации для индикатора
            var pulseAnimation = new DoubleAnimation
            {
                From = 1,
                To = 1.1,
                Duration = TimeSpan.FromMilliseconds(500),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            LoadingProgress.BeginAnimation(UIElement.OpacityProperty, pulseAnimation);
        });
    }

    private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        if (e.Error != null)
        {
            MessageBox.Show($"Ошибка: {e.Error.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
            return;
        }

        // Убеждаемся что финальная стадия показана
        SetFinalizingStage();

        // Даём время увидеть 100% и сообщение
        Dispatcher.Invoke(async () =>
        {
            await Task.Delay(300);
            _onCompleted();
            Close();
        });
    }

    public void ReportProgress(int percent, string status, string detail = "")
    {
        Dispatcher.Invoke(() =>
        {
            if (!string.IsNullOrEmpty(detail))
                _worker.ReportProgress(percent, (status, detail));
            else
                _worker.ReportProgress(percent, status);
        });
    }
}