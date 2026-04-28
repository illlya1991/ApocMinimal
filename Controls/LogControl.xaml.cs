using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ApocMinimal.Models;

namespace ApocMinimal.Controls;

public partial class LogControl : UserControl
{
    public enum TimeLevel { Day, Week, Month, Quarter, Year }

    private ScrollViewer? _scrollViewer;
    private bool _autoScroll = true;
    private TimeLevel _currentGroupLevel = TimeLevel.Day;
    private bool _settingLevel;

    private readonly List<LogDayItem> _days = new();
    private LogDayItem? _currentDay;
    private SectionItem? _npcGroup;
    private NpcSubItem? _currentNpcItem;
    private SectionItem? _systemSection;
    private SectionItem? _playerSection;
    private int _npcGroupCount;
    private int _currentNpcActionCount;
    private string _currentNpcSectionName = "";

    // ── Raw data (for persistence) ──────────────────────────────────────────
    private readonly List<LogDayData> _rawData = new();
    private LogDayData? _currentRaw;

    public List<LogDayData> GetAllRawData() => _rawData;
    public LogDayData? GetDayRaw(int dayNumber) => _rawData.FirstOrDefault(d => d.DayNumber == dayNumber);

    public LogControl()
    {
        InitializeComponent();

        if (LogScrollViewer != null)
        {
            LogScrollViewer.Loaded += (s, e) =>
            {
                _scrollViewer = LogScrollViewer;
                if (_scrollViewer != null)
                    _scrollViewer.ScrollChanged += OnScrollChanged;
            };
        }

        // Radio-style toggles — use Click to handle mutual exclusion
        if (WeekToggle != null)   WeekToggle.Click   += (s, e) => ToggleClicked(TimeLevel.Week,    WeekToggle.IsChecked   == true);
        if (MonthToggle != null)  MonthToggle.Click  += (s, e) => ToggleClicked(TimeLevel.Month,   MonthToggle.IsChecked  == true);
        if (QuarterToggle != null) QuarterToggle.Click += (s, e) => ToggleClicked(TimeLevel.Quarter, QuarterToggle.IsChecked == true);
        if (YearToggle != null)   YearToggle.Click   += (s, e) => ToggleClicked(TimeLevel.Year,    YearToggle.IsChecked   == true);
    }

    private void ToggleClicked(TimeLevel level, bool isNowChecked)
    {
        if (isNowChecked)
            SetTimeLevel(level);
        else
            SetTimeLevel(TimeLevel.Day);
    }

    private void SetTimeLevel(TimeLevel level)
    {
        if (_settingLevel) return;
        _settingLevel = true;
        _currentGroupLevel = level;
        if (WeekToggle    != null) WeekToggle.IsChecked    = level == TimeLevel.Week;
        if (MonthToggle   != null) MonthToggle.IsChecked   = level == TimeLevel.Month;
        if (QuarterToggle != null) QuarterToggle.IsChecked = level == TimeLevel.Quarter;
        if (YearToggle    != null) YearToggle.IsChecked    = level == TimeLevel.Year;
        _settingLevel = false;
        RebuildTimeHierarchy();
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_scrollViewer != null)
            _autoScroll = _scrollViewer.VerticalOffset >= _scrollViewer.ScrollableHeight - 10;
    }

    private void ScrollToBottom()
    {
        if (_autoScroll && _scrollViewer != null)
            Dispatcher.BeginInvoke(new System.Action(() => _scrollViewer?.ScrollToBottom()),
                System.Windows.Threading.DispatcherPriority.Background);
    }

    // ── Hierarchy rebuild ────────────────────────────────────────────────────

    private void RebuildTimeHierarchy()
    {
        // Detach all day containers from their current parents
        foreach (var day in _days)
        {
            if (day.Container?.Parent is Panel p)
                p.Children.Remove(day.Container);
        }

        LogStackPanel.Children.Clear();

        var orderedDays = _days.OrderBy(d => d.DayNumber).ToList();

        if (_currentGroupLevel == TimeLevel.Day)
        {
            foreach (var day in orderedDays)
                if (day.Container != null)
                    LogStackPanel.Children.Add(day.Container);
        }
        else
        {
            BuildNestedLevel(LogStackPanel, orderedDays, _currentGroupLevel);
        }

        ScrollToBottom();
    }

    private void BuildNestedLevel(Panel parent, List<LogDayItem> days, TimeLevel level)
    {
        var groups = days
            .GroupBy(d => GetGroupKey(d.DayNumber, level))
            .OrderBy(g => GetGroupSortKey(g.Key));

        foreach (var group in groups)
        {
            var groupContainer = CreateGroupContainer(group.Key, group.First().DayNumber, level);
            parent.Children.Add(groupContainer.Container!);

            var childLevel = GetChildLevel(level);
            if (childLevel == TimeLevel.Day)
            {
                foreach (var day in group.OrderBy(d => d.DayNumber))
                    if (day.Container != null)
                        groupContainer.ContentPanel!.Children.Add(day.Container);
            }
            else
            {
                BuildNestedLevel(groupContainer.ContentPanel!, group.OrderBy(d => d.DayNumber).ToList(), childLevel);
            }
        }
    }

    private static TimeLevel GetChildLevel(TimeLevel level) => level switch
    {
        TimeLevel.Year    => TimeLevel.Quarter,
        TimeLevel.Quarter => TimeLevel.Month,
        TimeLevel.Month   => TimeLevel.Week,
        TimeLevel.Week    => TimeLevel.Day,
        _                 => TimeLevel.Day,
    };

    private string GetGroupKey(int day, TimeLevel level) => level switch
    {
        TimeLevel.Week    => $"W{GameCalendar.GetWeek(day):D3}_{GameCalendar.GetYear(day)}",
        TimeLevel.Month   => $"M{GameCalendar.GetMonth(day):D2}_{GameCalendar.GetYear(day)}",
        TimeLevel.Quarter => $"Q{GameCalendar.GetQuarter(day)}_{GameCalendar.GetYear(day)}",
        TimeLevel.Year    => $"Y{GameCalendar.GetYear(day)}",
        _                 => $"D{day:D6}",
    };

    private static int GetGroupSortKey(string key)
    {
        if (key.StartsWith("Y"))  return int.Parse(key[1..]) * 1_000_000;
        if (key.StartsWith("Q")) { var p = key[1..].Split('_'); return int.Parse(p[1]) * 1_000_000 + int.Parse(p[0]) * 100_000; }
        if (key.StartsWith("M")) { var p = key[1..].Split('_'); return int.Parse(p[1]) * 1_000_000 + int.Parse(p[0]) * 1_000; }
        if (key.StartsWith("W")) { var p = key[1..].Split('_'); return int.Parse(p[1]) * 1_000_000 + int.Parse(p[0]) * 10; }
        if (key.StartsWith("D")) return int.TryParse(key[1..], out int v) ? v : 0;
        return 0;
    }

    private string FormatGroupHeader(string key, int sampleDay) => _currentGroupLevel switch
    {
        TimeLevel.Week    => $"📅 Неделя {GameCalendar.GetWeek(sampleDay)}  ({GameCalendar.GetMonthName(sampleDay)} {GameCalendar.GetYear(sampleDay)})",
        TimeLevel.Month   => $"📆 {GameCalendar.GetMonthName(sampleDay).ToUpperInvariant()}  {GameCalendar.GetYear(sampleDay)}",
        TimeLevel.Quarter => $"🗓 Квартал {GameCalendar.GetQuarter(sampleDay)}  ({GameCalendar.GetYear(sampleDay)})",
        TimeLevel.Year    => $"📂 {GameCalendar.GetYear(sampleDay)} год",
        _                 => ""
    };

    // For nested sub-groups the level passed may differ from _currentGroupLevel
    private string FormatSubGroupHeader(string key, int sampleDay, TimeLevel level) => level switch
    {
        TimeLevel.Week    => $"📅 Неделя {GameCalendar.GetWeek(sampleDay)}",
        TimeLevel.Month   => $"📆 {GameCalendar.GetMonthName(sampleDay)}",
        TimeLevel.Quarter => $"🗓 Квартал {GameCalendar.GetQuarter(sampleDay)}",
        TimeLevel.Year    => $"📂 {GameCalendar.GetYear(sampleDay)} год",
        _                 => ""
    };

    private GroupItem CreateGroupContainer(string key, int sampleDay, TimeLevel level)
    {
        bool isTopLevel = level == _currentGroupLevel;
        string header = isTopLevel ? FormatGroupHeader(key, sampleDay) : FormatSubGroupHeader(key, sampleDay, level);

        var container = new Border
        {
            Background  = GetBrush(isTopLevel ? "#0a0f18" : "#0d1117"),
            CornerRadius = new CornerRadius(4),
            Margin      = new Thickness(0, 0, 0, isTopLevel ? 8 : 4),
            BorderBrush = GetBrush(isTopLevel ? "#1f2d3d" : "#1a2030"),
            BorderThickness = new Thickness(isTopLevel ? 1 : 0),
        };
        var outer   = new StackPanel();
        var btn     = new Button { Content = header, Style = (Style)FindResource("TimeGroupButtonStyle") };
        var content = new StackPanel { Margin = new Thickness(isTopLevel ? 10 : 6, 4, 0, 6) };

        btn.Click += (s, e) =>
        {
            content.Visibility = content.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            UpdateArrow(btn, content.Visibility == Visibility.Visible);
        };
        UpdateArrow(btn, true);
        content.Visibility = Visibility.Visible;

        outer.Children.Add(btn);
        outer.Children.Add(content);
        container.Child = outer;

        return new GroupItem { Container = container, HeaderButton = btn, ContentPanel = content };
    }

    // ── Day creation ─────────────────────────────────────────────────────────

    public void NewDay(string header, int day)
    {
        CollapseCurrentDay();

        var dateStr    = GameCalendar.GetDateString(day);
        var weekday    = GameCalendar.GetWeekday(day);
        var weekdayShort = GameCalendar.GetWeekdayShort(day);
        var monthName  = GameCalendar.GetMonthName(day);
        var (week, month, season, year) = GameCalendar.GetTimeHierarchy(day);

        var dayContainer = new Border
        {
            Background      = GetBrush("#0d1117"),
            CornerRadius    = new CornerRadius(4),
            Margin          = new Thickness(0, 0, 0, 8),
        };
        var dayOuter = new StackPanel();

        // ── Button content ───────────────────────────────────────────────────
        // Row 1: "ДЕНЬ N — 1 марта 2026, понедельник"
        var headerLine = new TextBlock
        {
            Text       = $"ДЕНЬ {day}  —  {dateStr},  {weekday}",
            Foreground = GetBrush("#60a5fa"),
            FontSize   = 12,
            FontWeight = FontWeights.SemiBold,
        };

        // Row 2: small badges W/M/Q/Y
        var badgesRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };

        void AddBadge(string text, string bg, string fg, string tip)
        {
            badgesRow.Children.Add(new Border
            {
                Background   = GetBrush(bg),
                CornerRadius = new CornerRadius(3),
                Padding      = new Thickness(5, 1, 5, 1),
                Margin       = new Thickness(0, 0, 4, 0),
                ToolTip      = tip,
                Child        = new TextBlock { Text = text, Foreground = GetBrush(fg), FontSize = 9 }
            });
        }
        AddBadge($"W{week}",  "#1a2a3a", "#79c0ff", $"Неделя {week}");
        AddBadge(monthName[..Math.Min(3, monthName.Length)], "#2a1a3a", "#c084fc", monthName);
        AddBadge($"Q{GameCalendar.GetQuarter(day)}", "#1a3a2a", "#7ee787", $"Квартал {GameCalendar.GetQuarter(day)}");
        AddBadge(year.ToString(), "#3a2a1a", "#d29922", $"{year} год");

        var headerPanel = new StackPanel();
        headerPanel.Children.Add(headerLine);
        headerPanel.Children.Add(badgesRow);

        var dayHeaderBtn = new Button
        {
            Content  = headerPanel,
            Style    = (Style)FindResource("DayButtonStyle"),
            Padding  = new Thickness(10, 8, 10, 8),
            Tag      = day,
        };

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

        _currentDay = new LogDayItem
        {
            Container    = dayContainer,
            HeaderButton = dayHeaderBtn,
            ContentPanel = dayContent,
            DayNumber    = day,
        };
        _days.Add(_currentDay);

        // Raw data
        _currentRaw = new LogDayData { DayNumber = day };
        _rawData.Add(_currentRaw);

        _npcGroup      = CreateSection(dayContent, "🗡 Действия НПС",   "#f97316", true);
        _systemSection = CreateSection(dayContent, "⚙ Система",          "#60a5fa", true);
        _playerSection = CreateSection(dayContent, "🎮 Действия игрока", "#4ade80", false);

        _currentNpcItem       = null;
        _npcGroupCount        = 0;
        _currentNpcActionCount = 0;
        _currentNpcSectionName = "";

        if (_currentGroupLevel != TimeLevel.Day)
            RebuildTimeHierarchy();
        else
            LogStackPanel.Children.Add(dayContainer);

        ScrollToBottom();
    }

    public void NewDay(string header)
    {
        int day = 1;
        var m = Regex.Match(header, @"ДЕНЬ\s+(\d+)");
        if (m.Success && int.TryParse(m.Groups[1].Value, out int p)) day = p;
        NewDay(header, day);
    }

    // ── Section add methods ──────────────────────────────────────────────────

    public void BeginNpcSection(string npcName)
    {
        if (_npcGroup?.ContentPanel == null) return;
        _currentNpcActionCount = 0;
        _currentNpcSectionName = npcName;
        _npcGroupCount++;

        var subBtn   = new Button { Content = $"🧑 {npcName}", Style = (Style)FindResource("SectionButtonStyle") };
        var subPanel = new StackPanel { Margin = new Thickness(15, 2, 0, 4), Visibility = Visibility.Collapsed };
        UpdateArrow(subBtn, false);

        subBtn.Click += (s, e) =>
        {
            subPanel.Visibility = subPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            UpdateArrow(subBtn, subPanel.Visibility == Visibility.Visible);
        };

        _npcGroup.ContentPanel.Children.Add(subBtn);
        _npcGroup.ContentPanel.Children.Add(subPanel);
        _currentNpcItem = new NpcSubItem { Name = npcName, Button = subBtn, ContentPanel = subPanel };

        if (_npcGroup.HeaderButton != null)
            _npcGroup.HeaderButton.Content = $"🗡 Действия НПС [{_npcGroupCount}]";

        ScrollToBottom();
    }

    public void AddNpcEntry(string text, string color, bool isAction = false)
    {
        if (_currentNpcItem?.ContentPanel == null) return;
        AddText(_currentNpcItem.ContentPanel, text, color);

        if (isAction && _currentNpcItem.Button != null)
        {
            _currentNpcActionCount++;
            _currentNpcItem.Button.Content = $"🧑 {_currentNpcItem.Name} [{_currentNpcActionCount}]";
        }

        _currentRaw?.Entries.Add(new LogEntryData
        {
            Section  = $"npc:{_currentNpcSectionName}",
            Text     = text,
            Color    = color,
            IsAction = isAction,
        });

        ScrollToBottom();
    }

    public void AddSystemEntry(string text, string color)
    {
        if (_systemSection?.ContentPanel == null) return;
        if (_systemSection.ContentPanel.Visibility == Visibility.Collapsed && _systemSection.HeaderButton != null)
        {
            _systemSection.ContentPanel.Visibility = Visibility.Visible;
            UpdateArrow(_systemSection.HeaderButton, true);
        }
        AddText(_systemSection.ContentPanel, text, color);
        _currentRaw?.Entries.Add(new LogEntryData { Section = "system", Text = text, Color = color });
        ScrollToBottom();
    }

    public void AddEntry(string text, string color)
    {
        if (text.Contains("═══ ДЕНЬ") || text.Contains("=== День"))
        {
            NewDay(text);
            return;
        }
        if (_playerSection?.ContentPanel == null) return;
        AddText(_playerSection.ContentPanel, text, color);
        _currentRaw?.Entries.Add(new LogEntryData { Section = "player", Text = text, Color = color });
        ScrollToBottom();
    }

    // ── Rebuild from raw data (used when loading saved game) ─────────────────

    public void RebuildFromRaw(List<LogDayData> rawDays)
    {
        Clear();
        foreach (var dayRaw in rawDays.OrderBy(d => d.DayNumber))
        {
            NewDay($"ДЕНЬ {dayRaw.DayNumber}", dayRaw.DayNumber);

            string currentNpc = "";
            foreach (var entry in dayRaw.Entries)
            {
                if (entry.Section == "system")
                {
                    AddSystemEntry(entry.Text, entry.Color);
                }
                else if (entry.Section == "player")
                {
                    if (_playerSection?.ContentPanel != null)
                    {
                        AddText(_playerSection.ContentPanel, entry.Text, entry.Color);
                        // Don't re-add to _rawData (already rebuilt)
                        _currentRaw?.Entries.Add(new LogEntryData { Section = "player", Text = entry.Text, Color = entry.Color });
                    }
                }
                else if (entry.Section.StartsWith("npc:"))
                {
                    string npcName = entry.Section[4..];
                    if (npcName != currentNpc)
                    {
                        BeginNpcSection(npcName);
                        currentNpc = npcName;
                    }
                    AddNpcEntry(entry.Text, entry.Color, entry.IsAction);
                }
            }

            CollapseCurrentDay();
        }
        // Expand last day
        if (_currentDay?.ContentPanel != null && _currentDay.HeaderButton != null)
        {
            _currentDay.ContentPanel.Visibility = Visibility.Visible;
            UpdateArrow(_currentDay.HeaderButton, true);
        }
    }

    // ── Collapse/Expand helpers ──────────────────────────────────────────────

    public void CollapseCurrentDay()
    {
        if (_currentDay?.ContentPanel == null || _currentDay.HeaderButton == null) return;
        _currentDay.ContentPanel.Visibility = Visibility.Collapsed;
        UpdateArrow(_currentDay.HeaderButton, false);
    }

    public void CollapseAll()
    {
        foreach (var day in _days)
        {
            if (day.ContentPanel != null && day.HeaderButton != null)
            {
                day.ContentPanel.Visibility = Visibility.Collapsed;
                UpdateArrow(day.HeaderButton, false);
            }
        }
        CollapseAllGroups(LogStackPanel);
    }

    private void CollapseAllGroups(Panel panel)
    {
        foreach (UIElement child in panel.Children)
        {
            if (child is Border b && b.Child is StackPanel sp && sp.Children.Count >= 2
                && sp.Children[0] is Button btn && sp.Children[1] is StackPanel content)
            {
                content.Visibility = Visibility.Collapsed;
                UpdateArrow(btn, false);
                CollapseAllGroups(content);
            }
        }
    }

    public void ExpandAll()
    {
        foreach (var day in _days)
        {
            if (day.ContentPanel != null && day.HeaderButton != null)
            {
                day.ContentPanel.Visibility = Visibility.Visible;
                UpdateArrow(day.HeaderButton, true);
            }
        }
        ExpandAllGroups(LogStackPanel);
        ScrollToBottom();
    }

    private void ExpandAllGroups(Panel panel)
    {
        foreach (UIElement child in panel.Children)
        {
            if (child is Border b && b.Child is StackPanel sp && sp.Children.Count >= 2
                && sp.Children[0] is Button btn && sp.Children[1] is StackPanel content)
            {
                content.Visibility = Visibility.Visible;
                UpdateArrow(btn, true);
                ExpandAllGroups(content);
            }
        }
    }

    private void ExpandAllTimeBtn_Click(object sender, RoutedEventArgs e) => ExpandAll();
    private void CollapseAllTimeBtn_Click(object sender, RoutedEventArgs e) => CollapseAll();

    public void ShowLastDays(int daysCount)
    {
        int skip = Math.Max(0, _days.Count - daysCount);
        for (int i = 0; i < _days.Count; i++)
            if (_days[i].Container != null)
                _days[i].Container!.Visibility = i < skip ? Visibility.Collapsed : Visibility.Visible;
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
        _rawData.Clear();
        _currentDay    = null;
        _currentRaw    = null;
        _npcGroup      = null;
        _currentNpcItem = null;
        _systemSection = null;
        _playerSection = null;
        _npcGroupCount = 0;
        _currentNpcActionCount = 0;
        _currentNpcSectionName = "";
        _autoScroll    = true;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private SectionItem CreateSection(StackPanel parent, string title, string foregroundHex, bool collapsed)
    {
        var btn = new Button
        {
            Content  = title,
            Style    = (Style)FindResource("SectionButtonStyle"),
            Foreground = GetBrush(foregroundHex),
        };
        var panel = new StackPanel
        {
            Margin     = new Thickness(15, 2, 0, 4),
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
            Text        = text,
            Foreground  = GetBrush(color),
            FontFamily  = new FontFamily("Consolas"),
            FontSize    = 12,
            Margin      = new Thickness(0, 1, 0, 1),
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

    private void Clear_Click(object sender, RoutedEventArgs e)      => Clear();
    private void CollapseAll_Click(object sender, RoutedEventArgs e) => CollapseAll();
    private void ExpandAll_Click(object sender, RoutedEventArgs e)   => ExpandAll();
    private void Filter7Days_Click(object sender, RoutedEventArgs e) => ShowLastDays(7);
    private void ShowAll_Click(object sender, RoutedEventArgs e)     => ShowAllDays();

    // ── Internal classes ─────────────────────────────────────────────────────

    private class LogDayItem
    {
        public Border?     Container    { get; set; }
        public Button?     HeaderButton { get; set; }
        public StackPanel? ContentPanel { get; set; }
        public int         DayNumber    { get; set; }
    }

    private class SectionItem
    {
        public Button?     HeaderButton { get; set; }
        public StackPanel? ContentPanel { get; set; }
    }

    private class NpcSubItem
    {
        public string      Name         { get; set; } = "";
        public Button?     Button       { get; set; }
        public StackPanel? ContentPanel { get; set; }
    }

    private class GroupItem
    {
        public Border?     Container    { get; set; }
        public Button?     HeaderButton { get; set; }
        public StackPanel? ContentPanel { get; set; }
    }
}
