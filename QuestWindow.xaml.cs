using System.Windows;
using System.Windows.Controls;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.ViewModels;

namespace ApocMinimal;

public partial class QuestWindow : Window
{
    private readonly GameViewModel _vm;
    private readonly Action<string, string> _log;

    public QuestWindow(GameViewModel vm, Action<string, string> log)
    {
        InitializeComponent();
        _vm = vm;
        _log = log;
        Refresh();
    }

    private void Refresh()
    {
        _vm.ReloadQuestLibrary();

        ShopList.Items.Clear();
        foreach (var entry in _vm.QuestShop)
            ShopList.Items.Add($"{entry.Title}  [{entry.QuestTypeLabel}]  {entry.OvCost:F0} ОВ");

        LibraryList.Items.Clear();
        foreach (var entry in _vm.PurchasedQuests)
        {
            string title = entry.Catalog?.Title ?? $"#{entry.CatalogId}";
            LibraryList.Items.Add($"{title}  [{entry.PublishLabel}]");
        }

        PublishedList.Items.Clear();
        foreach (var q in _vm.PublishedQuests)
            PublishedList.Items.Add(q.Title);

        ActiveList.Items.Clear();
        foreach (var q in _vm.ActiveQuests)
        {
            var npc = _vm.GetNpcById(q.AssignedNpcId);
            string npcName = npc != null ? npc.Name : "?";
            ActiveList.Items.Add($"{q.Title}  [{npcName}, {q.DaysRemaining}д]");
        }

        CompletedList.Items.Clear();
        foreach (var q in _vm.CompletedQuests)
            CompletedList.Items.Add(q.Title);

        UpdateButtons();
        StatusText.Text = $"ОВ: {_vm.FaithPoints:F0}  |  Уровень алтаря: {_vm.AltarLevel}";
        ShopOvLabel.Text = $"Очки веры: {_vm.FaithPoints:F0}";
    }

    private void UpdateButtons()
    {
        BuyBtn.IsEnabled = ShopList.SelectedIndex >= 0;
        PublishBtn.IsEnabled = LibraryList.SelectedIndex >= 0 &&
            _vm.PurchasedQuests.Count > LibraryList.SelectedIndex &&
            _vm.PurchasedQuests[LibraryList.SelectedIndex].CanPublish;
        UnpublishBtn.IsEnabled = PublishedList.SelectedIndex >= 0;
        CollectAllBtn.IsEnabled = _vm.CompletedQuests.Count > 0;

        if (ShopList.SelectedIndex >= 0 && ShopList.SelectedIndex < _vm.QuestShop.Count)
        {
            var entry = _vm.QuestShop[ShopList.SelectedIndex];
            BuyBtn.Content = $"Купить ({entry.OvCost:F0} ОВ)";
        }
        else
        {
            BuyBtn.Content = "Купить";
        }
    }

    private void ShopList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateButtons();
        if (ShopList.SelectedIndex >= 0 && ShopList.SelectedIndex < _vm.QuestShop.Count)
        {
            var entry = _vm.QuestShop[ShopList.SelectedIndex];
            StatusText.Text = $"{entry.Title} — {entry.Description}  |  {entry.QuestTypeLabel}  |  Награда: {entry.RewardAmount:F0} {entry.RewardResource}";
        }
    }

    private void LibraryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateButtons();
        if (LibraryList.SelectedIndex >= 0 && LibraryList.SelectedIndex < _vm.PurchasedQuests.Count)
        {
            var entry = _vm.PurchasedQuests[LibraryList.SelectedIndex];
            string title = entry.Catalog?.Title ?? "";
            string desc = entry.Catalog?.Description ?? "";
            StatusText.Text = $"{title} — {desc}  |  Публикаций: {entry.PublishLabel}  |  Выполнено: {entry.TimesCompleted}";
        }
    }

    private void PublishedList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateButtons();
    }

    private void BuyBtn_Click(object sender, RoutedEventArgs e)
    {
        int idx = ShopList.SelectedIndex;
        if (idx < 0 || idx >= _vm.QuestShop.Count) return;
        var entry = _vm.QuestShop[idx];
        string result = _vm.BuyQuest(entry);
        _log(result, "#56d364");
        Refresh();
    }

    private void PublishBtn_Click(object sender, RoutedEventArgs e)
    {
        int idx = LibraryList.SelectedIndex;
        if (idx < 0 || idx >= _vm.PurchasedQuests.Count) return;
        var entry = _vm.PurchasedQuests[idx];
        string result = _vm.PublishQuest(entry);
        _log(result, "#58a6ff");
        Refresh();
    }

    private void UnpublishBtn_Click(object sender, RoutedEventArgs e)
    {
        int idx = PublishedList.SelectedIndex;
        if (idx < 0 || idx >= _vm.PublishedQuests.Count) return;
        var quest = _vm.PublishedQuests[idx];
        string result = _vm.UnpublishQuest(quest);
        _log(result, "#8b949e");
        Refresh();
    }

    private void CollectAllBtn_Click(object sender, RoutedEventArgs e)
    {
        var logs = _vm.CollectCompletedQuests();
        foreach (var line in logs)
            _log(line, "#22c55e");
        Refresh();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
