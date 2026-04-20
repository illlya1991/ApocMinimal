using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ApocMinimal.Models;

namespace ApocMinimal.Controls;

/// <summary>
/// Log control with 3 explicit sections per day:
///   1. Действия НПС  — NPC sub-sections (collapsed by default)
///   2. Система       — state summary at 23:00 (collapsed by default)
///   3. Действия игрока — player actions (expanded)
/// Supports time hierarchy grouping by week/month/season/year.
/// </summary>
public partial class LogControl : UserControl
{
    // Time grouping levels
    public enum TimeLevel { Day, Week, Month, Season, Year }

    private ScrollViewer? _scrollViewer;
    private bool _autoScroll = true;
    private TimeLevel _currentGroupLevel = TimeLevel.Day;

    private readonly List<LogDayItem> _days = new();
    private LogDayItem? _currentDay;
    private SectionItem? _npcGroup;
    private NpcSubItem? _currentNpcItem;
    private SectionItem? _systemSection;
    private SectionItem? _playerSection;
    private int _npcGroupCount;
    private int _currentNpcActionCount;

    // Time grouping structures
    private readonly Dictionary<string, LogTimeGroup> _timeGroups = new();

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

        // Initialize time hierarchy toggles
        if (WeekToggle != null) WeekToggle.Checked += (s, e) => SetTimeLevel(TimeLevel.Week);
        if (MonthToggle != null) MonthToggle.Checked += (s, e) => SetTimeLevel(TimeLevel.Month);
        if (SeasonToggle != null) SeasonToggle.Checked += (s, e) => SetTimeLevel(TimeLevel.Season);
        if (YearToggle != null) YearToggle.Checked += (s, e) => SetTimeLevel(TimeLevel.Year);

        if (WeekToggle != null) WeekToggle.Unchecked += (s, e) => { if (!IsAnyTimeLevelChecked()) SetTimeLevel(TimeLevel.Day); };
        if (MonthToggle != null) MonthToggle.Unchecked += (s, e) => { if (!IsAnyTimeLevelChecked()) SetTimeLevel(TimeLevel.Day); };
        if (SeasonToggle != null) SeasonToggle.Unchecked += (s, e) => { if (!IsAnyTimeLevelChecked()) SetTimeLevel(TimeLevel.Day); };
        if (YearToggle != null) YearToggle.Unchecked += (s, e) => { if (!IsAnyTimeLevelChecked()) SetTimeLevel(TimeLevel.Day); };
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

    private bool IsAnyTimeLevelChecked()
    {
        return (WeekToggle != null && WeekToggle.IsChecked == true) ||
               (MonthToggle != null && MonthToggle.IsChecked == true) ||
               (SeasonToggle != null && SeasonToggle.IsChecked == true) ||
               (YearToggle != null && YearToggle.IsChecked == true);
    }

    private void SetTimeLevel(TimeLevel level)
    {
        if (_currentGroupLevel == level) return;
        _currentGroupLevel = level;
        RebuildTimeHierarchy();
    }

    private void ExpandAllTimeBtn_Click(object sender, RoutedEventArgs e)
    {
        foreach (var group in _timeGroups.Values)
        {
            if (group.ContentPanel != null && group.HeaderButton != null)
            {
                group.ContentPanel.Visibility = Visibility.Visible;
                UpdateArrow(group.HeaderButton, true);
            }
        }
    }

    private void CollapseAllTimeBtn_Click(object sender, RoutedEventArgs e)
    {
        foreach (var group in _timeGroups.Values)
        {
            if (group.ContentPanel != null && group.HeaderButton != null)
            {
                group.ContentPanel.Visibility = Visibility.Collapsed;
                UpdateArrow(group.HeaderButton, false);
            }
        }
    }

    private void RebuildTimeHierarchy()
    {
        // Save current day states
        var dayStates = new Dictionary<int, bool>();
        foreach (var day in _days)
        {
            if (day.ContentPanel != null)
                dayStates[day.DayNumber] = day.ContentPanel.Visibility == Visibility.Visible;
        }

        LogStackPanel.Children.Clear();
        _timeGroups.Clear();

        // Group days by selected time level
        var groupedDays = _days
            .Where(d => d.Container?.Visibility == Visibility.Visible)
            .GroupBy(d => GetGroupKey(d.DayNumber))
            .OrderBy(g => GetGroupSortOrder(g.Key))
            .ToList();

        foreach (var group in groupedDays)
        {
            var timeGroup = CreateTimeGroup(group.Key, group.First().DayNumber);
            _timeGroups[group.Key] = timeGroup;
            LogStackPanel.Children.Add(timeGroup.Container);

            // Add days to this group
            foreach (var day in group.OrderBy(d => d.DayNumber))
            {
                if (day.Container != null && day.ContentPanel != null)
                {
                    // Remove from original parent and add to group
                    if (day.Container.Parent is Panel oldParent)
                        oldParent.Children.Remove(day.Container);

                    if (timeGroup.ContentPanel != null)
                        timeGroup.ContentPanel.Children.Add(day.Container);

                    // Restore expanded state if needed
                    if (dayStates.TryGetValue(day.DayNumber, out bool wasExpanded) && wasExpanded && day.HeaderButton != null)
                    {
                        day.ContentPanel.Visibility = Visibility.Visible;
                        UpdateArrow(day.HeaderButton, true);
                    }

                    timeGroup.Days.Add(day);
                }
            }
        }

        ScrollToBottom();
    }

    private string GetGroupKey(int day)
    {
        var date = GameCalendar.GetDate(day);
        return _currentGroupLevel switch
        {
            TimeLevel.Week => $"W{GameCalendar.GetWeek(day)}_{date.Year}",
            TimeLevel.Month => $"M{date.Month}_{date.Year}",
            TimeLevel.Season => $"S{GetSeasonNumber(day)}_{date.Year}",
            TimeLevel.Year => $"Y{date.Year}",
            _ => $"D{day}"
        };
    }

    private int GetSeasonNumber(int day)
    {
        string season = GameCalendar.GetSeason(day);
        return season switch
        {
            "Зима" => 0,
            "Весна" => 1,
            "Лето" => 2,
            "Осень" => 3,
            _ => 0
        };
    }

    private int GetGroupSortOrder(string groupKey)
    {
        if (groupKey.StartsWith("Y"))
            return int.Parse(groupKey.Substring(1)) * 10000;
        if (groupKey.StartsWith("S"))
        {
            var parts = groupKey.Substring(1).Split('_');
            return int.Parse(parts[1]) * 100 + int.Parse(parts[0]) * 10;
        }
        if (groupKey.StartsWith("M"))
        {
            var parts = groupKey.Substring(1).Split('_');
            return int.Parse(parts[1]) * 100 + int.Parse(parts[0]);
        }
        if (groupKey.StartsWith("W"))
        {
            var parts = groupKey.Substring(1).Split('_');
            return int.Parse(parts[1]) * 100 + int.Parse(parts[0]);
        }
        return int.Parse(groupKey.Substring(1));
    }

    private string FormatGroupHeader(string groupKey, int sampleDay)
    {
        return _currentGroupLevel switch
        {
            TimeLevel.Week => $"📅 Неделя {GameCalendar.GetWeek(sampleDay)} ({GameCalendar.GetMonthName(sampleDay)} {GameCalendar.GetYear(sampleDay)})",
            TimeLevel.Month => $"📆 {GameCalendar.GetMonthName(sampleDay)} {GameCalendar.GetYear(sampleDay)}",
            TimeLevel.Season => $"🌤 {GameCalendar.GetSeason(sampleDay)} {GameCalendar.GetYear(sampleDay)}",
            TimeLevel.Year => $"📂 {GameCalendar.GetYear(sampleDay)} год",
            _ => ""
        };
    }

    private LogTimeGroup CreateTimeGroup(string groupKey, int sampleDay)
    {
        string header = FormatGroupHeader(groupKey, sampleDay);
        bool expanded = _currentGroupLevel == TimeLevel.Day;

        var container = new Border
        {
            Background = GetBrush("#0d1117"),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(0, 0, 0, 8),
        };

        var outer = new StackPanel();
        var headerBtn = new Button
        {
            Content = header,
            Style = (Style)FindResource("TimeGroupButtonStyle"),
        };
        var content = new StackPanel { Margin = new Thickness(10, 4, 0, 6) };

        headerBtn.Click += (s, e) =>
        {
            content.Visibility = content.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            UpdateArrow(headerBtn, content.Visibility == Visibility.Visible);
        };
        UpdateArrow(headerBtn, expanded);
        content.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;

        outer.Children.Add(headerBtn);
        outer.Children.Add(content);
        container.Child = outer;

        return new LogTimeGroup
        {
            Container = container,
            HeaderButton = headerBtn,
            ContentPanel = content,
            GroupKey = groupKey
        };
    }

    /// <summary>Start a new day — creates 3 ordered sections with date information.</summary>
    public void NewDay(string header, int day)
    {
        CollapseCurrentDay();

        var dateInfo = GameCalendar.GetDateString(day);
        var weekdayFull = GameCalendar.GetWeekday(day);
        var (week, month, season, year) = GameCalendar.GetTimeHierarchy(day);
        var monthName = GameCalendar.GetMonthName(day);

        var dayContainer = new Border
        {
            Background = GetBrush("#0d1117"),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(0, 0, 0, 8),
        };
        var dayOuter = new StackPanel();

        var headerPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 2) };

        var mainHeaderText = new TextBlock
        {
            Text = header,
            Foreground = GetBrush("#60a5fa"),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
        };
        headerPanel.Children.Add(mainHeaderText);

        var subHeaderPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 0) };

        var dateText = new TextBlock
        {
            Text = $"{dateInfo}",
            Foreground = GetBrush("#8b949e"),
            FontSize = 10,
        };
        subHeaderPanel.Children.Add(dateText);

        var weekdayText = new TextBlock
        {
            Text = $" • {weekdayFull}",
            Foreground = GetBrush("#6b7280"),
            FontSize = 10,
        };
        subHeaderPanel.Children.Add(weekdayText);

        var hierarchyPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 0, 0, 0) };

        var weekBadge = new Border
        {
            Background = GetBrush("#1a2a3a"),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(6, 1, 6, 1),
            Margin = new Thickness(0, 0, 4, 0),
            Child = new TextBlock
            {
                Text = $"W{week}",
                Foreground = GetBrush("#79c0ff"),
                FontSize = 9,
                ToolTip = $"Неделя {week}"
            }
        };
        hierarchyPanel.Children.Add(weekBadge);

        var monthBadge = new Border
        {
            Background = GetBrush("#2a1a3a"),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(6, 1, 6, 1),
            Margin = new Thickness(0, 0, 4, 0),
            Child = new TextBlock
            {
                Text = monthName.Substring(0, Math.Min(3, monthName.Length)),
                Foreground = GetBrush("#c084fc"),
                FontSize = 9,
                ToolTip = monthName
            }
        };
        hierarchyPanel.Children.Add(monthBadge);

        var seasonBadge = new Border
        {
            Background = GetBrush("#1a3a2a"),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(6, 1, 6, 1),
            Margin = new Thickness(0, 0, 4, 0),
            Child = new TextBlock
            {
                Text = GetSeasonIcon(season),
                Foreground = GetBrush("#7ee787"),
                FontSize = 9,
                ToolTip = season
            }
        };
        hierarchyPanel.Children.Add(seasonBadge);

        var yearBadge = new Border
        {
            Background = GetBrush("#3a2a1a"),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(6, 1, 6, 1),
            Child = new TextBlock
            {
                Text = year.ToString(),
                Foreground = GetBrush("#d29922"),
                FontSize = 9,
                ToolTip = $"{year} год"
            }
        };
        hierarchyPanel.Children.Add(yearBadge);

        subHeaderPanel.Children.Add(hierarchyPanel);
        headerPanel.Children.Add(subHeaderPanel);

        var dayHeaderBtn = new Button
        {
            Content = headerPanel,
            Style = (Style)FindResource("DayButtonStyle"),
            Padding = new Thickness(10, 8, 10, 8),
            Tag = day
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
            Container = dayContainer,
            HeaderButton = dayHeaderBtn,
            ContentPanel = dayContent,
            DayNumber = day,
            DateInfo = dateInfo,
            Weekday = weekdayFull
        };
        _days.Add(_currentDay);

        // Create sections
        _npcGroup = CreateSection(dayContent, "🗡 Действия НПС", "#f97316", true);
        _systemSection = CreateSection(dayContent, "⚙ Система", "#60a5fa", true);
        _playerSection = CreateSection(dayContent, "🎮 Действия игрока", "#4ade80", false);

        _currentNpcItem = null;
        _npcGroupCount = 0;
        _currentNpcActionCount = 0;

        // If we're grouping, rebuild the hierarchy
        if (_currentGroupLevel != TimeLevel.Day)
        {
            RebuildTimeHierarchy();
        }
        else
        {
            LogStackPanel.Children.Add(dayContainer);
        }

        ScrollToBottom();
    }

    private static string GetSeasonIcon(string season) => season switch
    {
        "Зима" => "❄️",
        "Весна" => "🌸",
        "Лето" => "☀️",
        "Осень" => "🍂",
        _ => "📅"
    };

    public void NewDay(string header)
    {
        int day = 1;
        var match = Regex.Match(header, @"ДЕНЬ\s+(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int parsedDay))
        {
            day = parsedDay;
        }
        NewDay(header, day);
    }

    public void BeginNpcSection(string npcName)
    {
        if (_npcGroup == null || _npcGroup.ContentPanel == null) return;

        _currentNpcActionCount = 0;
        _npcGroupCount++;

        var subBtn = new Button
        {
            Content = $"🧑 {npcName}",
            Style = (Style)FindResource("SectionButtonStyle"),
        };
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
        {
            _npcGroup.HeaderButton.Content = $"🗡 Действия НПС [{_npcGroupCount}]";
            UpdateArrow(_npcGroup.HeaderButton, _npcGroup.ContentPanel.Visibility == Visibility.Visible);
        }

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
        ScrollToBottom();
    }

    public void CollapseCurrentDay()
    {
        if (_currentDay?.ContentPanel == null || _currentDay.HeaderButton == null) return;
        _currentDay.ContentPanel.Visibility = Visibility.Collapsed;
        UpdateArrow(_currentDay.HeaderButton, false);
    }

    public void CollapseAll()
    {
        if (_currentGroupLevel != TimeLevel.Day)
        {
            foreach (var group in _timeGroups.Values)
            {
                if (group.ContentPanel != null && group.HeaderButton != null)
                {
                    group.ContentPanel.Visibility = Visibility.Collapsed;
                    UpdateArrow(group.HeaderButton, false);
                }
            }
        }
        else
        {
            foreach (var day in _days)
            {
                if (day.ContentPanel != null && day.HeaderButton != null)
                {
                    day.ContentPanel.Visibility = Visibility.Collapsed;
                    UpdateArrow(day.HeaderButton, false);
                }
            }
        }
    }

    public void ExpandAll()
    {
        if (_currentGroupLevel != TimeLevel.Day)
        {
            foreach (var group in _timeGroups.Values)
            {
                if (group.ContentPanel != null && group.HeaderButton != null)
                {
                    group.ContentPanel.Visibility = Visibility.Visible;
                    UpdateArrow(group.HeaderButton, true);
                }
            }
        }
        else
        {
            foreach (var day in _days)
            {
                if (day.ContentPanel != null && day.HeaderButton != null)
                {
                    day.ContentPanel.Visibility = Visibility.Visible;
                    UpdateArrow(day.HeaderButton, true);
                }
            }
        }
        ScrollToBottom();
    }

    public void ShowLastDays(int daysCount)
    {
        int daysToShow = Math.Min(daysCount, _days.Count);
        var daysToHide = _days.Take(_days.Count - daysToShow).ToList();

        if (_currentGroupLevel != TimeLevel.Day)
        {
            foreach (var group in _timeGroups.Values)
            {
                bool hasVisible = false;
                foreach (var day in group.Days)
                {
                    bool shouldShow = daysToShow >= _days.Count - _days.IndexOf(day);
                    if (day.Container != null)
                        day.Container.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;
                    if (shouldShow) hasVisible = true;
                }
                if (group.Container != null)
                    group.Container.Visibility = hasVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        else
        {
            foreach (var day in daysToHide)
                if (day.Container != null) day.Container.Visibility = Visibility.Collapsed;
            foreach (var day in _days.Skip(_days.Count - daysToShow))
                if (day.Container != null) day.Container.Visibility = Visibility.Visible;
        }
        ScrollToBottom();
    }

    public void ShowAllDays()
    {
        if (_currentGroupLevel != TimeLevel.Day)
        {
            foreach (var group in _timeGroups.Values)
            {
                foreach (var day in group.Days)
                    if (day.Container != null) day.Container.Visibility = Visibility.Visible;
                if (group.Container != null) group.Container.Visibility = Visibility.Visible;
            }
        }
        else
        {
            foreach (var day in _days)
                if (day.Container != null) day.Container.Visibility = Visibility.Visible;
        }
        ScrollToBottom();
    }

    public void Clear()
    {
        LogStackPanel.Children.Clear();
        _days.Clear();
        _timeGroups.Clear();
        _currentDay = null;
        _npcGroup = null;
        _currentNpcItem = null;
        _systemSection = null;
        _playerSection = null;
        _npcGroupCount = 0;
        _currentNpcActionCount = 0;
        _autoScroll = true;
    }

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
            FontFamily = new FontFamily("Consolas"),
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

    private void Clear_Click(object sender, RoutedEventArgs e) => Clear();
    private void CollapseAll_Click(object sender, RoutedEventArgs e) => CollapseAll();
    private void ExpandAll_Click(object sender, RoutedEventArgs e) => ExpandAll();
    private void Filter7Days_Click(object sender, RoutedEventArgs e) => ShowLastDays(7);
    private void ShowAll_Click(object sender, RoutedEventArgs e) => ShowAllDays();

    private class LogDayItem
    {
        public Border? Container { get; set; }
        public Button? HeaderButton { get; set; }
        public StackPanel? ContentPanel { get; set; }
        public int DayNumber { get; set; }
        public string DateInfo { get; set; } = "";
        public string Weekday { get; set; } = "";
    }

    private class SectionItem
    {
        public Button? HeaderButton { get; set; }
        public StackPanel? ContentPanel { get; set; }
    }

    private class NpcSubItem
    {
        public string Name { get; set; } = "";
        public Button? Button { get; set; }
        public StackPanel? ContentPanel { get; set; }
    }

    private class LogTimeGroup
    {
        public Border? Container { get; set; }
        public Button? HeaderButton { get; set; }
        public StackPanel? ContentPanel { get; set; }
        public string GroupKey { get; set; } = "";
        public List<LogDayItem> Days { get; set; } = new();
    }
}