using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ApocMinimal.Controls;

/// <summary>
/// Элемент лога - одна строка
/// </summary>
public class LogEntryItem
{
    public string Text { get; set; } = "";
    public string ColorHex { get; set; } = "#c9d1d9";
}

/// <summary>
/// Секция лога (например, "Действия" или "Информация об NPC")
/// </summary>
public class LogSection
{
    public string Title { get; set; } = "";
    public string Icon { get; set; } = "";
    public List<LogEntryItem> Items { get; set; } = new();
    public bool IsExpanded { get; set; } = true;
    public Button? HeaderButton { get; set; }
    public StackPanel? ContentPanel { get; set; }
}

/// <summary>
/// День в логе
/// </summary>
public class LogDay
{
    public string Header { get; set; } = "";
    public List<LogSection> Sections { get; set; } = new();
    public bool IsExpanded { get; set; } = true;
    public Border? Container { get; set; }
    public Button? HeaderButton { get; set; }
    public StackPanel? ContentPanel { get; set; }
}

/// <summary>
/// Контрол для отображения лога с аккордеон-стилем
/// </summary>
public partial class LogControl : UserControl
{
    private readonly List<LogDay> _logDays = new();
    private ScrollViewer? _scrollViewer;
    private bool _autoScroll = true;

    private LogDay? _currentDay;
    private LogSection? _currentPlayerSection;
    private LogSection? _currentNpcSection;
    private LogSection? _currentSystemSection;

    public LogControl()
    {
        InitializeComponent();

        LogScrollViewer.Loaded += (s, e) =>
        {
            _scrollViewer = LogScrollViewer;
            _scrollViewer.ScrollChanged += OnScrollChanged;
        };
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_scrollViewer != null)
        {
            bool isAtBottom = _scrollViewer.VerticalOffset >= _scrollViewer.ScrollableHeight - 10;
            _autoScroll = isAtBottom;
        }
    }

    private void ScrollToBottom()
    {
        if (_autoScroll && _scrollViewer != null)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _scrollViewer.ScrollToBottom();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }

    /// <summary>
    /// Начать новый день
    /// </summary>
    public void NewDay(string header)
    {
        // Создаем контейнер для дня
        var dayContainer = new Border
        {
            Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#0d1117")!,
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(0, 0, 0, 8),
        };

        // Кнопка-заголовок дня
        var headerButton = new Button
        {
            Content = header,
            Style = (Style)FindResource("DayButtonStyle"),
        };

        // Контент дня (будет скрываться/показываться)
        var dayContent = new StackPanel { Margin = new Thickness(10, 4, 0, 6) };

        headerButton.Click += (s, e) =>
        {
            dayContent.Visibility = dayContent.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        };

        dayContainer.Child = new StackPanel();
        ((StackPanel)dayContainer.Child).Children.Add(headerButton);
        ((StackPanel)dayContainer.Child).Children.Add(dayContent);

        LogStackPanel.Children.Add(dayContainer);

        _currentDay = new LogDay
        {
            Header = header,
            Container = dayContainer,
            HeaderButton = headerButton,
            ContentPanel = dayContent,
            IsExpanded = true
        };
        _logDays.Add(_currentDay);

        _currentPlayerSection = null;
        _currentNpcSection = null;
        _currentSystemSection = null;

        ScrollToBottom();
    }

    private void EnsureCurrentSection(ref LogSection? section, string title, string icon)
    {
        if (_currentDay == null)
        {
            NewDay("=== День 1 ===");
        }

        if (section == null)
        {
            section = new LogSection { Title = title, Icon = icon, IsExpanded = true };

            // Создаем кнопку секции
            var sectionButton = new Button
            {
                Content = $"{icon} {title}",
                Style = (Style)FindResource("SectionButtonStyle"),
            };

            // Контент секции
            var sectionContent = new StackPanel { Margin = new Thickness(15, 2, 0, 4) };
            sectionContent.Visibility = Visibility.Visible;

            sectionButton.Click += (s, e) =>
            {
                sectionContent.Visibility = sectionContent.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            };

            section.HeaderButton = sectionButton;
            section.ContentPanel = sectionContent;

            _currentDay!.ContentPanel?.Children.Add(sectionButton);
            _currentDay.ContentPanel?.Children.Add(sectionContent);

            _currentDay.Sections.Add(section);
        }
    }

    private void AddTextToSection(LogSection? section, string text, string colorHex)
    {
        if (section?.ContentPanel == null) return;

        var textBlock = new TextBlock
        {
            Text = text,
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(colorHex)!,
            Style = (Style)FindResource("LogTextStyle"),
        };

        section.ContentPanel.Children.Add(textBlock);
    }

    /// <summary>
    /// Добавить строку в секцию игрока
    /// </summary>
    public void AddPlayerAction(string text, string colorHex)
    {
        EnsureCurrentSection(ref _currentPlayerSection, "Действия", "🎮");
        _currentPlayerSection?.Items.Add(new LogEntryItem { Text = text, ColorHex = colorHex });
        AddTextToSection(_currentPlayerSection, text, colorHex);
        ScrollToBottom();
    }

    /// <summary>
    /// Добавить строку в секцию NPC
    /// </summary>
    public void AddNpcInfo(string text, string colorHex)
    {
        EnsureCurrentSection(ref _currentNpcSection, "Информация об NPC", "📋");
        _currentNpcSection?.Items.Add(new LogEntryItem { Text = text, ColorHex = colorHex });
        AddTextToSection(_currentNpcSection, text, colorHex);
        ScrollToBottom();
    }

    /// <summary>
    /// Добавить разделитель в секцию NPC
    /// </summary>
    public void AddNpcSeparator()
    {
        if (_currentNpcSection != null)
        {
            var separator = "──────────────────────────────────────────────";
            _currentNpcSection.Items.Add(new LogEntryItem { Text = separator, ColorHex = "#60a5fa" });
            AddTextToSection(_currentNpcSection, separator, "#60a5fa");
            ScrollToBottom();
        }
    }

    /// <summary>
    /// Добавить системное сообщение
    /// </summary>
    public void AddSystemMessage(string text, string colorHex)
    {
        EnsureCurrentSection(ref _currentSystemSection, "Система", "⚙");
        _currentSystemSection?.Items.Add(new LogEntryItem { Text = text, ColorHex = colorHex });
        AddTextToSection(_currentSystemSection, text, colorHex);
        ScrollToBottom();
    }

    /// <summary>
    /// Очистить весь лог
    /// </summary>
    public void Clear()
    {
        LogStackPanel.Children.Clear();
        _logDays.Clear();
        _currentDay = null;
        _currentPlayerSection = null;
        _currentNpcSection = null;
        _currentSystemSection = null;
        _autoScroll = true;
    }

    /// <summary>
    /// Свернуть все дни
    /// </summary>
    public void CollapseAll()
    {
        foreach (var day in _logDays)
        {
            if (day.ContentPanel != null)
                day.ContentPanel.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Развернуть все дни
    /// </summary>
    public void ExpandAll()
    {
        foreach (var day in _logDays)
        {
            if (day.ContentPanel != null)
                day.ContentPanel.Visibility = Visibility.Visible;
        }
        ScrollToBottom();
    }

    /// <summary>
    /// Показать только последние N дней
    /// </summary>
    public void ShowLastDays(int daysCount)
    {
        int daysToShow = Math.Min(daysCount, _logDays.Count);
        var daysToHide = _logDays.SkipLast(daysToShow).ToList();

        foreach (var day in daysToHide)
        {
            if (day.Container != null)
                day.Container.Visibility = Visibility.Collapsed;
        }

        var daysToShowList = _logDays.TakeLast(daysToShow).ToList();
        foreach (var day in daysToShowList)
        {
            if (day.Container != null)
                day.Container.Visibility = Visibility.Visible;
        }

        ScrollToBottom();
    }

    /// <summary>
    /// Показать все дни
    /// </summary>
    public void ShowAllDays()
    {
        foreach (var day in _logDays)
        {
            if (day.Container != null)
                day.Container.Visibility = Visibility.Visible;
        }
        ScrollToBottom();
    }

    /// <summary>
    /// Показать информацию об NPC (массовое добавление)
    /// </summary>
    public void ShowNpcInfo(List<(string text, string color)> lines)
    {
        bool oldAutoScroll = _autoScroll;
        _autoScroll = true;

        EnsureCurrentSection(ref _currentNpcSection, "Информация об NPC", "📋");

        foreach (var line in lines)
        {
            _currentNpcSection?.Items.Add(new LogEntryItem { Text = line.text, ColorHex = line.color });
            AddTextToSection(_currentNpcSection, line.text, line.color);
        }

        _autoScroll = oldAutoScroll;
        ScrollToBottom();
    }

    // =========================================================
    // Event Handlers
    // =========================================================

    private void Clear_Click(object sender, RoutedEventArgs e) => Clear();

    private void CollapseAll_Click(object sender, RoutedEventArgs e) => CollapseAll();

    private void ExpandAll_Click(object sender, RoutedEventArgs e) => ExpandAll();

    private void Filter7Days_Click(object sender, RoutedEventArgs e) => ShowLastDays(7);

    private void ShowAll_Click(object sender, RoutedEventArgs e) => ShowAllDays();
}