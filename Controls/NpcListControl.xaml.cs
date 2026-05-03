using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Services;

namespace ApocMinimal.Controls;

public partial class NpcListControl : UserControl
{
    public event Action<Npc>? NpcSelected;

    private readonly GameUIService _uiService;
    private List<Npc> _allNpcs = new();
    private List<int> _controlledZoneIds = new();
    private Npc? _currentSelectedNpc;

    private bool _showFollowers = true; // true = followers tab, false = territory tab

    private int _currentPage = 1;
    private const int PageSize = 20;
    private int _totalPages = 1;
    private List<Npc> _filteredNpcs = new();

    public NpcListControl()
    {
        InitializeComponent();
        _uiService = new GameUIService((text, color) => { });
        InitFilters();
    }

    private void InitFilters()
    {
        FilterFollower.Items.Add("Все уровни");
        for (int i = 1; i <= 5; i++) FilterFollower.Items.Add(i.ToString());
        FilterFollower.SelectedIndex = 0;

        FilterEvolution.Items.Add("Все");
        for (int i = 0; i <= 5; i++) FilterEvolution.Items.Add(i.ToString());
        FilterEvolution.SelectedIndex = 0;
    }

    public void UpdateNpcs(List<Npc> npcs, List<int> controlledZoneIds, Npc? selectedNpc = null)
    {
        _allNpcs = npcs ?? new();
        _controlledZoneIds = controlledZoneIds ?? new();
        _currentSelectedNpc = selectedNpc;
        _currentPage = 1;
        ApplyFilters();
    }

    // Overload for backwards compatibility
    public void UpdateNpcs(List<Npc> npcs, Npc? selectedNpc = null)
        => UpdateNpcs(npcs, _controlledZoneIds, selectedNpc);

    private void ApplyFilters()
    {
        if (_showFollowers)
        {
            var followerFilter = FilterFollower.SelectedIndex; // 0=all, 1-5=level
            var evolutionFilter = FilterEvolution.SelectedIndex - 1; // -1=all, 0-5=level

            _filteredNpcs = _allNpcs
                .Where(n => n.IsAlive && n.FollowerLevel >= 1)
                .Where(n => followerFilter == 0 || n.FollowerLevel == followerFilter)
                .Where(n => evolutionFilter < 0 || n.EvolutionLevel == evolutionFilter)
                .OrderByDescending(n => n.FollowerLevel)
                .ThenByDescending(n => n.EvolutionLevel)
                .ThenBy(n => n.Name)
                .ToList();

            FilterPanel.Visibility = Visibility.Visible;
            TabCountLabel.Text = $"{_filteredNpcs.Count} последователей";

            int total = _allNpcs.Count(n => n.IsAlive && n.FollowerLevel >= 1);
            FollowerCountLabel.Text = total != _filteredNpcs.Count ? $"всего: {total}" : "";
        }
        else
        {
            var zoneSet = _controlledZoneIds.ToHashSet();
            _filteredNpcs = _allNpcs
                .Where(n => n.IsAlive && zoneSet.Contains(n.LocationId))
                .OrderByDescending(n => n.FollowerLevel)
                .ThenBy(n => n.Name)
                .ToList();

            FilterPanel.Visibility = Visibility.Collapsed;
            TabCountLabel.Text = $"{_filteredNpcs.Count} НПС на территории";
            FollowerCountLabel.Text = "";
        }

        _totalPages = _filteredNpcs.Count > 0
            ? (int)System.Math.Ceiling((double)_filteredNpcs.Count / PageSize)
            : 1;
        if (_currentPage > _totalPages) _currentPage = _totalPages;

        RefreshCurrentPage();
        UpdatePaginationButtons();
    }

    private void RefreshCurrentPage()
    {
        NpcPanel.Children.Clear();

        int startIndex = (_currentPage - 1) * PageSize;
        if (startIndex >= _filteredNpcs.Count && _filteredNpcs.Count > 0)
        {
            _currentPage = 1;
            startIndex = 0;
        }

        if (_filteredNpcs.Count == 0)
        {
            NpcPanel.Children.Add(new TextBlock
            {
                Text = _showFollowers ? "Нет последователей" : "Никого на территории",
                Foreground = BrushCache.GetBrush("#8b949e"),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            });
            PageInfoLabel.Text = "0";
            return;
        }

        var page = _filteredNpcs.Skip(startIndex).Take(PageSize).ToList();
        foreach (var npc in page)
        {
            var card = _uiService.BuildNpcCard(npc);
            var captured = npc;
            card.MouseLeftButtonUp += (_, _) => NpcSelected?.Invoke(captured);

            if (_currentSelectedNpc != null && _currentSelectedNpc.Id == npc.Id)
            {
                card.BorderBrush = BrushCache.GetBrush("#60a5fa")!;
                card.BorderThickness = new Thickness(2);
            }

            NpcPanel.Children.Add(card);
        }

        int from = startIndex + 1;
        int to = startIndex + page.Count;
        PageInfoLabel.Text = $"{from}-{to} / {_filteredNpcs.Count}";
    }

    private void UpdatePaginationButtons()
    {
        PrevPageBtn.IsEnabled = _currentPage > 1;
        NextPageBtn.IsEnabled = _currentPage < _totalPages;
        PageInfoLabel.Foreground = BrushCache.GetBrush(_filteredNpcs.Count > 0 ? "#60a5fa" : "#8b949e")!;
    }

    private void Tab_Click(object sender, RoutedEventArgs e)
    {
        _showFollowers = sender == TabFollowers;
        TabFollowers.IsChecked = _showFollowers;
        TabTerritory.IsChecked = !_showFollowers;
        _currentPage = 1;
        ApplyFilters();
    }

    private void Filter_Changed(object sender, SelectionChangedEventArgs e)
    {
        _currentPage = 1;
        ApplyFilters();
    }

    private void PrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 1)
        {
            _currentPage--;
            RefreshCurrentPage();
            UpdatePaginationButtons();
            if (NpcPanel.Parent is ScrollViewer sv) sv.ScrollToTop();
        }
    }

    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage < _totalPages)
        {
            _currentPage++;
            RefreshCurrentPage();
            UpdatePaginationButtons();
            if (NpcPanel.Parent is ScrollViewer sv) sv.ScrollToTop();
        }
    }

    public void Clear()
    {
        _allNpcs.Clear();
        _filteredNpcs.Clear();
        NpcPanel.Children.Clear();
        TabCountLabel.Text = "0 НПС";
        FollowerCountLabel.Text = "";
        PageInfoLabel.Text = "0";
        PrevPageBtn.IsEnabled = false;
        NextPageBtn.IsEnabled = false;
    }
}
