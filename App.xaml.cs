using System;
using System.Runtime;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ApocMinimal;

public partial class App : Application
{
    private readonly System.Diagnostics.Stopwatch _appSw = new();

    public App()
    {
        _appSw.Start();
        System.Diagnostics.Debug.WriteLine($"=== ПРИЛОЖЕНИЕ ЗАПУЩЕНО: {DateTime.Now:HH:mm:ss.fff} ===");

        ThreadPool.SetMinThreads(Environment.ProcessorCount * 4, Environment.ProcessorCount * 2);
        GCSettings.LatencyMode = GCLatencyMode.Batch;

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;

        System.Diagnostics.Debug.WriteLine($"  Инициализация App: {_appSw.ElapsedMilliseconds} мс");
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"  OnStartup: {_appSw.ElapsedMilliseconds} мс");
        base.OnStartup(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ShowErrorDialog(e.Exception);
        e.Handled = true;
    }

    private void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            ShowErrorDialog(ex);
    }

    private void OnTaskSchedulerUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        ShowErrorDialog(e.Exception);
        e.SetObserved();
    }

    private static void ShowErrorDialog(Exception ex)
    {
        var message = $"Ошибка:\n{ex.GetType().Name}\n{ex.Message}";

        Application.Current?.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        });
    }
}