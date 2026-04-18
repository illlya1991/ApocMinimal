using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ApocMinimal.Controls;

/// <summary>
/// Log control with 3 explicit sections per day:
///   1. Действия НПС  — NPC sub-sections (collapsed by default)
///   2. Система       — state summary at 23:00 (collapsed by default)
///   3. Действия игрока — player actions (expanded)
/// </summary>
public partial class LogControl : UserControl
{
    private ScrollViewer? _scrollViewer;
    private bool _autoScroll = true;

    private readonly List<LogDayItem> _days = new();
    private LogDayItem?    _currentDay;
    private SectionItem?   _npcGroup;        // "Действия НПС"
    private NpcSubItem?    _currentNpcItem;  // current NPC inside npcGroup
    private SectionItem?   _systemSection;   // "Система"
    private SectionItem?   _playerSection;   // "Действия игрока"
    private int _npcGroupCount;
    private int _currentNpcActionCount;

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
            _autoScroll = _scrollViewer.VerticalOffset >= _scrollViewer.ScrollableHeight - 10;
    }

    private void ScrollToBottom()
    {
        if (_autoScroll && _scrollViewer != null)
            Dispatcher.BeginInvoke(new System.Action(() => _scrollViewer.ScrollToBottom()),
                System.Windows.Threading.DispatcherPriority.Background);
    }

    // =========================================================
    // Public API
    // =========================================================

    /// <summary>Start a new day — creates 3 ordered sections.</summary>
    public void NewDay(string header)
    {
        CollapseCurrentDay();

        var dayContainer = new Border
        {
            Background = GetBrush("#0d1117"),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(0, 0, 0, 8),
        };
        var dayOuter = new StackPanel();
        var dayHeaderBtn = new Button { Content = header, Style = (Style)FindResource("DayButtonStyle") };
        var dayContent = new StackPanel { Margin = new Thickness(10, 4, 0, 6) };

        dayHeaderBtn.Click += (s, e) =>
        {
            dayContent.Visibility = dayContent.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            UpdateArrow(dayHeaderBtn, dayContent.Visibility == Visibility.Visible);
        };
        UpdateArrow(dayHeaderBtn, true);

        dayOuter.Children.Add(dayHeaderBtn);
        dayOuter.Children.Add(dayContent);
        dayContainer.Child = dayOuter;
        LogStackPanel.Children.Add(dayContainer);

        _currentDay = new LogDayItem { Container = dayContainer, HeaderButton = dayHeaderBtn, ContentPanel = dayContent };
        _days.Add(_currentDay);

        // Pre-create 3 sections in correct display order
        _npcGroup      = CreateSection(dayContent, "🗡 Действия НПС", "#f97316", collapsed: true);
        _systemSection = CreateSection(dayContent, "⚙ Система",       "#60a5fa", collapsed: true);
        _playerSection = CreateSection(dayContent, "🎮 Действия игрока","#4ade80", collapsed: false);

        _currentNpcItem        = null;
        _npcGroupCount         = 0;
        _currentNpcActionCount = 0;

        ScrollToBottom();
    }

    /// <summary>Begin a new NPC sub-section inside "Действия НПС".</summary>
    public void BeginNpcSection(string npcName)
    {
        if (_npcGroup == null) return;

        _currentNpcActionCount = 0;
        _npcGroupCount++;

        var subBtn = new Button
        {
            Content = $"🧑 {npcName}",
            Style   = (Style)FindResource("SectionButtonStyle"),
        };
        var subPanel = new StackPanel { Margin = new Thickness(15, 2, 0, 4), Visibility = Visibility.Collapsed };
        UpdateArrow(subBtn, false);

        subBtn.Click += (s, e) =>
        {
            subPanel.Visibility = subPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            UpdateArrow(subBtn, subPanel.Visibility == Visibility.Visible);
        };

        _npcGroup.ContentPanel!.Children.Add(subBtn);
        _npcGroup.ContentPanel.Children.Add(subPanel);

        _currentNpcItem = new NpcSubItem { Name = npcName, Button = subBtn, ContentPanel = subPanel };

        // Update group header count
        _npcGroup.HeaderButton!.Content = $"🗡 Действия НПС [{_npcGroupCount}]";
        UpdateArrow(_npcGroup.HeaderButton, _npcGroup.ContentPanel.Visibility == Visibility.Visible);

        ScrollToBottom();
    }

    /// <summary>Add a line to the current NPC sub-section. isAction=true counts towards action counter.</summary>
    public void AddNpcEntry(string text, string color, bool isAction = false)
    {
        if (_currentNpcItem == null) return;
        AddText(_currentNpcItem.ContentPanel!, text, color);

        if (isAction)
        {
            _currentNpcActionCount++;
            _currentNpcItem.Button!.Content = $"🧑 {_currentNpcItem.Name} [{_currentNpcActionCount}]";
        }

        ScrollToBottom();
    }

    /// <summary>Add a line to the "Система" section.</summary>
    public void AddSystemEntry(string text, string color)
    {
        if (_systemSection == null) return;
        AddText(_systemSection.ContentPanel!, text, color);
        ScrollToBottom();
    }

    /// <summary>Add a line to the "Действия игрока" section (default for player actions).</summary>
    public void AddEntry(string text, string color)
    {
        if (text.Contains("═══ ДЕНЬ") || text.Contains("=== День"))
        {
            NewDay(text);
            return;
        }
        if (_playerSection == null) return;
        AddText(_playerSection.ContentPanel!, text, color);
        ScrollToBottom();
    }

    // =========================================================
    // Collapse / Expand
    // =========================================================

    public void CollapseCurrentDay()
    {
        if (_currentDay?.ContentPanel == null) return;
        _currentDay.ContentPanel.Visibility = Visibility.Collapsed;
        UpdateArrow(_currentDay.HeaderButton!, false);
    }

    public void CollapseAll()
    {
        foreach (var day in _days)
        {
            if (day.ContentPanel == null) continue;
            day.ContentPanel.Visibility = Visibility.Collapsed;
            UpdateArrow(day.HeaderButton!, false);
        }
    }

    public void ExpandAll()
    {
        foreach (var day in _days)
        {
            if (day.ContentPanel == null) continue;
            day.ContentPanel.Visibility = Visibility.Visible;
            UpdateArrow(day.HeaderButton!, true);
        }
        ScrollToBottom();
    }

    public void ShowLastDays(int daysCount)
    {
        int daysToShow = System.Math.Min(daysCount, _days.Count);
        foreach (var day in _days.SkipLast(daysToShow))
            if (day.Container != null) day.Container.Visibility = Visibility.Collapsed;
        foreach (var day in _days.TakeLast(daysToShow))
            if (day.Container != null) day.Container.Visibility = Visibility.Visible;
        ScrollToBottom();
    }

    public void ShowAllDays()
    {
        foreach (var day in _days)
            if (day.Container != null) day.Container.Visibility = Visibility.Visible;
        ScrollToBottom();
    }

    public void Clear()
    {
        LogStackPanel.Children.Clear();
        _days.Clear();
        _currentDay        = null;
        _npcGroup          = null;
        _currentNpcItem    = null;
        _systemSection     = null;
        _playerSection     = null;
        _npcGroupCount         = 0;
        _currentNpcActionCount = 0;
        _autoScroll = true;
    }

    // =========================================================
    // Helpers
    // =========================================================

    private SectionItem CreateSection(StackPanel parent, string title, string foregroundHex, bool collapsed)
    {
        var btn = new Button
        {
            Content = title,
            Style = (Style)FindResource("SectionButtonStyle"),
            Foreground = GetBrush(foregroundHex),
        };
        var panel = new StackPanel
        {
            Margin = new Thickness(15, 2, 0, 4),
            Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible,
        };
        UpdateArrow(btn, !collapsed);

        btn.Click += (s, e) =>
        {
            panel.Visibility = panel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            UpdateArrow(btn, panel.Visibility == Visibility.Visible);
        };

        parent.Children.Add(btn);
        parent.Children.Add(panel);

        return new SectionItem { HeaderButton = btn, ContentPanel = panel };
    }

    private static void AddText(StackPanel panel, string text, string color)
    {
        panel.Children.Add(new TextBlock
        {
            Text = text,
            Foreground = GetBrush(color),
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            FontSize = 12,
            Margin = new Thickness(0, 1, 0, 1),
            TextWrapping = TextWrapping.Wrap,
        });
    }

    private static void UpdateArrow(Button button, bool isExpanded)
    {
        if (button.Template?.FindName("ArrowText", button) is TextBlock arrow)
            arrow.Text = isExpanded ? "▼ " : "▶ ";
    }

    private static SolidColorBrush GetBrush(string hex) =>
        (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;

    // =========================================================
    // Event handlers
    // =========================================================

    private void Clear_Click(object sender, RoutedEventArgs e) => Clear();
    private void CollapseAll_Click(object sender, RoutedEventArgs e) => CollapseAll();
    private void ExpandAll_Click(object sender, RoutedEventArgs e) => ExpandAll();
    private void Filter7Days_Click(object sender, RoutedEventArgs e) => ShowLastDays(7);
    private void ShowAll_Click(object sender, RoutedEventArgs e) => ShowAllDays();

    // =========================================================
    // Internal types
    // =========================================================

    private class LogDayItem
    {
        public Border?     Container    { get; set; }
        public Button?     HeaderButton { get; set; }
        public StackPanel? ContentPanel { get; set; }
    }

    private class SectionItem
    {
        public Button?     HeaderButton { get; set; }
        public StackPanel? ContentPanel { get; set; }
    }

    private class NpcSubItem
    {
        public string     Name         { get; set; } = "";
        public Button?    Button       { get; set; }
        public StackPanel? ContentPanel { get; set; }
    }
}
