using System.ComponentModel;
using System.Windows;

namespace ApocMinimal;

public partial class LoadingWindow : Window
{
    private readonly BackgroundWorker _worker;
    private readonly Action<BackgroundWorker, DoWorkEventArgs> _workAction;
    private readonly Action _onCompleted;

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

        _worker.DoWork             += Worker_DoWork;
        _worker.ProgressChanged    += Worker_ProgressChanged;
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
        if (pct >= 0)
        {
            LoadingProgress.IsIndeterminate = false;
            LoadingProgress.Value = pct;
            PercentText.Text = $"{pct}%";
        }

        if (e.UserState is string message)
        {
            StatusText.Text = message;
            DetailText.Text = "";
        }
        else if (e.UserState is ValueTuple<string, string> tuple)
        {
            StatusText.Text = tuple.Item1;
            DetailText.Text = tuple.Item2;
        }
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

        _onCompleted();
        Close();
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
