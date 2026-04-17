using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.ViewModels;

namespace ApocMinimal;

public class PublishedQuestRow
{
    public Quest Quest { get; }
    public QuestCatalogEntry? Catalog { get; }

    public PublishedQuestRow(Quest quest, QuestCatalogEntry? catalog)
    {
        Quest = quest;
        Catalog = catalog;
    }

    public string Title => Quest.Title;

    public string TakeCondLabelDisplay => Catalog?.TakeCondLabel ?? "—";
    public string CompletionLabelDisplay => Catalog?.CompletionLabel ?? $"{Quest.DaysRequired} дн.";
    public string RewardLabelDisplay => Catalog?.RewardLabel ?? "—";
}

public class ActiveQuestRow
{
    public Quest Quest { get; }
    private readonly string _npcName;

    public ActiveQuestRow(Quest quest, string npcName)
    {
        Quest = quest;
        _npcName = npcName;
    }

    public string Title => Quest.Title;
    public string NpcNameDisplay => _npcName;
    public int DaysRemaining => Quest.DaysRemaining;
    public double CompletionPercent => Quest.CompletionPercent;
    public bool IsReadyDisplay => Quest.CompletionPercent >= 100;
}

public partial class QuestWindow : Window
{
    private readonly GameViewModel _vm;
    private readonly Action<string, string> _log;
    private List<QuestCatalogEntry> _shopItems = new();

    public QuestWindow(GameViewModel viewModel, Action<string, string> log)
    {
        InitializeComponent();
        _vm = viewModel;
        _log = log;
        PopulateAltarFilter();
        Refresh();
    }

    private void PopulateAltarFilter()
    {
        AltarFilterCombo.Items.Clear();
        AltarFilterCombo.Items.Add("Все уровни");
        for (int i = 1; i <= 10; i++)
            AltarFilterCombo.Items.Add($"Алтарь {i}+");
        AltarFilterCombo.SelectedIndex = 0;
    }

    private void Refresh()
    {
        _vm.ReloadQuestLibrary();
        RefreshShop();
        RefreshPurchased();
        RefreshPublished();
        RefreshActive();
        RefreshHistory();
        StatusText.Text = $"ОВ: {_vm.FaithPoints:F0}  |  Алтарь: {_vm.AltarLevel}  |  Куплено: {_vm.PurchasedQuests.Count}  |  Опубликовано: {_vm.PublishedQuests.Count}";
    }

    private void RefreshShop()
    {
        int altarFilter = AltarFilterCombo.SelectedIndex;
        var catalog = _vm.QuestShop;

        var ownedOneTime = new HashSet<int>();
        var ownedEternal = new HashSet<int>();
        foreach (var lib in _vm.PurchasedQuests)
        {
            if (lib.QuestType == QuestType.OneTime) ownedOneTime.Add(lib.CatalogId);
            if (lib.QuestType == QuestType.Eternal) ownedEternal.Add(lib.CatalogId);
        }

        _shopItems = altarFilter <= 0
            ? catalog
            : catalog.Where(e => e.MinAltarLevel == altarFilter).ToList();

        _shopItems = _shopItems
            .Where(e => !ownedEternal.Contains(e.Id))
            .ToList();

        ShopGrid.ItemsSource = null;
        ShopGrid.ItemsSource = _shopItems;

        for (int i = 0; i < _shopItems.Count; i++)
        {
            var item = _shopItems[i];
            if (ownedOneTime.Contains(item.Id))
            {
                var row = ShopGrid.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                if (row != null)
                {
                    row.Opacity = 0.4;
                    row.IsEnabled = false;
                }
            }
        }
    }

    private void RefreshPurchased()
    {
        PurchasedGrid.ItemsSource = null;
        PurchasedGrid.ItemsSource = _vm.PurchasedQuests;
    }

    private void RefreshPublished()
    {
        var catalog = _vm.QuestShop.Concat(_vm.PurchasedQuests
            .Where(p => p.Catalog != null)
            .Select(p => p.Catalog!))
            .DistinctBy(c => c.Id)
            .ToDictionary(c => c.Id);

        var libMap = _vm.PurchasedQuests.ToDictionary(e => e.Id);

        var rows = _vm.PublishedQuests.Select(q =>
        {
            QuestCatalogEntry? cat = null;
            if (q.LibraryId > 0 && libMap.TryGetValue(q.LibraryId, out var lib) && lib.Catalog != null)
                cat = lib.Catalog;
            return new PublishedQuestRow(q, cat);
        }).ToList();

        PublishedGrid.ItemsSource = null;
        PublishedGrid.ItemsSource = rows;
    }

    private void RefreshActive()
    {
        var rows = _vm.ActiveQuests.Select(q =>
        {
            var npc = _vm.GetNpcById(q.AssignedNpcId);
            return new ActiveQuestRow(q, npc?.Name ?? "—");
        }).ToList();

        ActiveGrid.ItemsSource = null;
        ActiveGrid.ItemsSource = rows;
    }

    private void RefreshHistory()
    {
        HistoryTree.Items.Clear();
        var history = _vm.QuestHistory;

        var grouped = history.GroupBy(h => h.QuestTitle).OrderBy(g => g.Key);
        foreach (var group in grouped)
        {
            var parent = new TreeViewItem
            {
                Header = $"{group.Key} (×{group.Count()})",
                Foreground = new SolidColorBrush(Color.FromRgb(0x58, 0xa6, 0xff)),
                IsExpanded = false,
            };

            foreach (var entry in group)
            {
                string npcPart = string.IsNullOrEmpty(entry.NpcName) ? "" : $"{entry.NpcName} | ";
                string reward = string.IsNullOrEmpty(entry.RewardGiven) ? "" : $" | {entry.RewardGiven}";
                var child = new TreeViewItem
                {
                    Header = $"{npcPart}День {entry.DayTaken}→{entry.DayCompleted}{reward}",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x8b, 0x94, 0x9e)),
                };
                parent.Items.Add(child);
            }

            HistoryTree.Items.Add(parent);
        }
    }

    private void AltarFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshShop();
    }

    private void ShopGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ShopGrid.SelectedItem is QuestCatalogEntry entry)
        {
            var parts = new List<string>();
            if (entry.PriceOneTime.HasValue) parts.Add($"×1: {entry.PriceOneTime:F0} ОВ");
            if (entry.PriceRepeatable.HasValue) parts.Add($"×10: {entry.PriceRepeatable:F0} ОВ");
            if (entry.PriceEternal.HasValue) parts.Add($"∞: {entry.PriceEternal:F0} ОВ");
            string prices = string.Join("  ", parts);
            StatusText.Text = $"{entry.Title} — {entry.Description}  |  {prices}  |  Условие: {entry.TakeCondLabel}  |  Выполнение: {entry.CompletionLabel}  |  Награда: {entry.RewardLabel}";
        }
    }

    private void PurchasedGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PurchasedGrid.SelectedItem is PlayerLibraryEntry lib && lib.Catalog != null)
        {
            StatusText.Text = $"{lib.Catalog.Title} — {lib.Catalog.Description}  |  Публикаций: {lib.PublishLabel}  |  Выполнено: {lib.TimesCompleted}  |  Тип: {lib.QuestType}";
        }
    }

    private void BuyOneTime_Click(object sender, RoutedEventArgs e)
    {
        if (ShopGrid.SelectedItem is not QuestCatalogEntry entry) return;
        if (entry.PriceOneTime == null) { _log("Покупка ×1 недоступна для этого квеста", "#f87171"); return; }
        var result = _vm.BuyQuest(entry, QuestType.OneTime);
        _log(result, "#56d364");
        Refresh();
    }

    private void BuyRepeatable_Click(object sender, RoutedEventArgs e)
    {
        if (ShopGrid.SelectedItem is not QuestCatalogEntry entry) return;
        if (entry.PriceRepeatable == null) { _log("Покупка ×10 недоступна для этого квеста", "#f87171"); return; }
        var result = _vm.BuyQuest(entry, QuestType.Repeatable);
        _log(result, "#56d364");
        Refresh();
    }

    private void BuyEternal_Click(object sender, RoutedEventArgs e)
    {
        if (ShopGrid.SelectedItem is not QuestCatalogEntry entry) return;
        if (entry.PriceEternal == null) { _log("Покупка ∞ недоступна для этого квеста", "#f87171"); return; }
        var result = _vm.BuyQuest(entry, QuestType.Eternal);
        _log(result, "#d2a8ff");
        Refresh();
    }

    private void Publish_Click(object sender, RoutedEventArgs e)
    {
        if (PurchasedGrid.SelectedItem is not PlayerLibraryEntry entry) return;
        var result = _vm.PublishQuest(entry);
        _log(result, "#58a6ff");
        Refresh();
    }

    private void Unpublish_Click(object sender, RoutedEventArgs e)
    {
        if (PublishedGrid.SelectedItem is not PublishedQuestRow row) return;
        var result = _vm.UnpublishQuest(row.Quest);
        _log(result, "#8b949e");
        Refresh();
    }

    private void CollectAll_Click(object sender, RoutedEventArgs e)
    {
        var logs = _vm.CollectCompletedQuests();
        foreach (var line in logs)
            _log(line, "#22c55e");
        Refresh();
    }

    private void ShopGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ShopGrid.SelectedItem is not QuestCatalogEntry entry) return;
        ShowQuestInfo(entry.Title, entry.Description, entry.TakeCondLabel, entry.CompletionLabel, entry.RewardLabel);
    }

    private void PurchasedGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (PurchasedGrid.SelectedItem is not PlayerLibraryEntry lib || lib.Catalog == null) return;
        ShowQuestInfo(lib.Catalog.Title, lib.Catalog.Description, lib.Catalog.TakeCondLabel, lib.Catalog.CompletionLabel, lib.Catalog.RewardLabel);
    }

    private void PublishedGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (PublishedGrid.SelectedItem is not PublishedQuestRow row) return;
        string cond = row.TakeCondLabelDisplay;
        string comp = row.CompletionLabelDisplay;
        string rew = row.RewardLabelDisplay;
        ShowQuestInfo(row.Title, row.Quest.Description, cond, comp, rew);
    }

    private void ActiveGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ActiveGrid.SelectedItem is not ActiveQuestRow row) return;
        ShowQuestInfo(row.Title, row.Quest.Description,
            "—",
            $"{row.DaysRemaining} дн. (выполнено {row.CompletionPercent:F0}%)",
            "—");
    }

    private static void ShowQuestInfo(string title, string description, string cond, string completion, string reward)
    {
        string msg = $"Название: {title}\n\n{description}\n\nУсловия взятия: {cond}\nВыполнение: {completion}\nНаграда: {reward}";
        MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
