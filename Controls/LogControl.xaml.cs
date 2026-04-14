using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ApocMinimal.Controls;

/// <summary>
/// Контрол для отображения лога с поддержкой сворачиваемых блоков
/// </summary>
public partial class LogControl : UserControl
{
    private ScrollViewer? _scrollViewer;
    private bool _autoScroll = true;

    // Структуры данных
    private readonly List<LogDayItem> _days = new();
    private LogDayItem? _currentDay;
    private LogSectionItem? _currentPlayerSection;
    private LogSectionItem? _currentNpcSection;
    private LogSectionItem? _currentSystemSection;

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
        var dayContainer = new Border
        {
            Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#0d1117")!,
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(0, 0, 0, 8),
        };

        var headerButton = new Button
        {
            Content = header,
            Style = (Style)FindResource("DayButtonStyle"),
            Tag = "day",
        };

        var dayContent = new StackPanel { Margin = new Thickness(10, 4, 0, 6) };
        dayContent.Visibility = Visibility.Visible;

        // Обновляем стрелку при клике
        headerButton.Click += (s, e) =>
        {
            dayContent.Visibility = dayContent.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            UpdateButtonArrow(headerButton, dayContent.Visibility == Visibility.Visible);
        };

        dayContainer.Child = new StackPanel();
        ((StackPanel)dayContainer.Child).Children.Add(headerButton);
        ((StackPanel)dayContainer.Child).Children.Add(dayContent);

        LogStackPanel.Children.Add(dayContainer);

        _currentDay = new LogDayItem
        {
            Header = header,
            Container = dayContainer,
            HeaderButton = headerButton,
            ContentPanel = dayContent,
            IsExpanded = true
        };
        _days.Add(_currentDay);

        _currentPlayerSection = null;
        _currentNpcSection = null;
        _currentSystemSection = null;

        ScrollToBottom();
    }

    private void UpdateButtonArrow(Button button, bool isExpanded)
    {
        if (button.Template?.FindName("ArrowText", button) is TextBlock arrow)
        {
            arrow.Text = isExpanded ? "▼ " : "▶ ";
        }
    }

    private void EnsureCurrentSection(ref LogSectionItem? section, string title, string icon)
    {
        if (_currentDay == null)
        {
            NewDay("=== День 1 ===");
        }

        if (section == null)
        {
            section = new LogSectionItem { Title = title, Icon = icon, IsExpanded = true };

            var sectionButton = new Button
            {
                Content = $"{icon} {title}",
                Style = (Style)FindResource("SectionButtonStyle"),
                Tag = "section",
            };

            var sectionContent = new StackPanel { Margin = new Thickness(15, 2, 0, 4) };
            sectionContent.Visibility = Visibility.Visible;

            sectionButton.Click += (s, e) =>
            {
                sectionContent.Visibility = sectionContent.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                UpdateButtonArrow(sectionButton, sectionContent.Visibility == Visibility.Visible);
            };

            section.HeaderButton = sectionButton;
            section.ContentPanel = sectionContent;

            _currentDay!.ContentPanel?.Children.Add(sectionButton);
            _currentDay.ContentPanel?.Children.Add(sectionContent);

            _currentDay.Sections.Add(section);
        }
    }

    private void AddTextToSection(LogSectionItem? section, string text, string colorHex)
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
    /// Добавить запись в лог (автоматически определяет секцию)
    /// </summary>
    public void AddEntry(string text, string colorHex)
    {
        // Определяем тип сообщения
        if (text.Contains("═══ ДЕНЬ") || text.Contains("=== День"))
        {
            NewDay(text);
        }
        else if (text.Contains("──") || text.Contains("────────────────"))
        {
            EnsureCurrentSection(ref _currentNpcSection, "Информация об NPC", "📋");
            AddTextToSection(_currentNpcSection, text, colorHex);
        }
        else if (text.Contains("Алтарь") || text.Contains("веры") || text.Contains("ОВ") ||
                 text.Contains("алтарь") || text.Contains("Вера") || text.Contains("Система") ||
                 text.Contains("Мир загружен") || text.Contains("Выживших:"))
        {
            EnsureCurrentSection(ref _currentSystemSection, "Система", "⚙");
            AddTextToSection(_currentSystemSection, text, colorHex);
        }
        else if (text.Contains("ПОТРЕБНОСТИ") || text.Contains("ПАМЯТЬ") || text.Contains("ХАРАКТЕРИСТИКИ") ||
                 text.Contains("ФИЗИЧЕСКИЕ") || text.Contains("МЕНТАЛЬНЫЕ") || text.Contains("ЭНЕРГЕТИЧЕСКИЕ") ||
                 text.Contains("HP:") || text.Contains("Чакра:") || text.Contains("Эмоции:") ||
                 text.Contains("Черты:") || text.Contains("Цель:") || text.Contains("Мечта:"))
        {
            EnsureCurrentSection(ref _currentNpcSection, "Информация об NPC", "📋");
            AddTextToSection(_currentNpcSection, text, colorHex);
        }
        else
        {
            EnsureCurrentSection(ref _currentPlayerSection, "Действия", "🎮");
            AddTextToSection(_currentPlayerSection, text, colorHex);
        }

        ScrollToBottom();
    }

    /// <summary>
    /// Очистить лог
    /// </summary>
    public void Clear()
    {
        LogStackPanel.Children.Clear();
        _days.Clear();
        _currentDay = null;
        _currentPlayerSection = null;
        _currentNpcSection = null;
        _currentSystemSection = null;
        _autoScroll = true;
    }

    /// <summary>
    /// Свернуть все дни и секции
    /// </summary>
    public void CollapseAll()
    {
        foreach (var day in _days)
        {
            if (day.ContentPanel != null)
            {
                day.ContentPanel.Visibility = Visibility.Collapsed;
                UpdateButtonArrow(day.HeaderButton!, false);
            }

            foreach (var section in day.Sections)
            {
                if (section.ContentPanel != null)
                {
                    section.ContentPanel.Visibility = Visibility.Collapsed;
                    UpdateButtonArrow(section.HeaderButton!, false);
                }
            }
        }
    }

    /// <summary>
    /// Развернуть все дни и секции
    /// </summary>
    public void ExpandAll()
    {
        foreach (var day in _days)
        {
            if (day.ContentPanel != null)
            {
                day.ContentPanel.Visibility = Visibility.Visible;
                UpdateButtonArrow(day.HeaderButton!, true);
            }

            foreach (var section in day.Sections)
            {
                if (section.ContentPanel != null)
                {
                    section.ContentPanel.Visibility = Visibility.Visible;
                    UpdateButtonArrow(section.HeaderButton!, true);
                }
            }
        }
        ScrollToBottom();
    }

    /// <summary>
    /// Показать только последние N дней
    /// </summary>
    public void ShowLastDays(int daysCount)
    {
        int daysToShow = System.Math.Min(daysCount, _days.Count);
        var daysToHide = _days.SkipLast(daysToShow).ToList();

        foreach (var day in daysToHide)
        {
            if (day.Container != null)
                day.Container.Visibility = Visibility.Collapsed;
        }

        var daysToShowList = _days.TakeLast(daysToShow).ToList();
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
        foreach (var day in _days)
        {
            if (day.Container != null)
                day.Container.Visibility = Visibility.Visible;
        }
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

    // Внутренние классы
    private class LogSectionItem
    {
        public string Title { get; set; } = "";
        public string Icon { get; set; } = "";
        public bool IsExpanded { get; set; } = true;
        public Button? HeaderButton { get; set; }
        public StackPanel? ContentPanel { get; set; }
    }

    private class LogDayItem
    {
        public string Header { get; set; } = "";
        public List<LogSectionItem> Sections { get; set; } = new();
        public bool IsExpanded { get; set; } = true;
        public Border? Container { get; set; }
        public Button? HeaderButton { get; set; }
        public StackPanel? ContentPanel { get; set; }
    }
}