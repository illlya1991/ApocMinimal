using System.Windows;
using System.Windows.Controls;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.TechniqueData;
using ApocMinimal.ViewModels;

namespace ApocMinimal;

// ── Row types ──────────────────────────────────────────────────────────────

public class TechCatalogRow
{
    public Technique Tech { get; }
    public TechCatalogRow(Technique t) => Tech = t;

    public string Name          => Tech.Name;
    public int    TerminalLevel => Tech.TerminalLevel;
    public string TechType      => Tech.TechType.ToString();
    public double OPCost        => Tech.OPCost;
    public string ModesLabel    => Tech.ModesLabel;
    public string Description   => Tech.Description;
    public string Faction       => Tech.Faction;
}

public class TechInventoryRow
{
    public Technique Tech  { get; }
    public int       Count { get; }
    public TechInventoryRow(Technique t, int count) { Tech = t; Count = count; }

    public string Name          => Tech.Name;
    public int    TerminalLevel => Tech.TerminalLevel;
    public string TechType      => Tech.TechType.ToString();
    public string CatalogKey    => Tech.CatalogKey;
}

public class NpcTechRow
{
    public Technique Tech { get; }
    public NpcTechRow(Technique t) => Tech = t;

    public string Name          => Tech.Name;
    public int    TerminalLevel => Tech.TerminalLevel;
    public string TechType      => Tech.TechType.ToString();
}

// ── Window ─────────────────────────────────────────────────────────────────

public partial class TechniqueWindow : Window
{
    private readonly GameViewModel _vm;
    private readonly Action<string, string> _log;
    private List<TechCatalogRow> _catalogRows = new();

    public TechniqueWindow(GameViewModel vm, Action<string, string> log)
    {
        InitializeComponent();
        _vm = vm;
        _log = log;

        PopulateFactionFilter();
        PopulateLevelFilter();
        PopulateNpcCombo();
        Refresh();
    }

    // ── Init helpers ────────────────────────────────────────────────────────

    private void PopulateFactionFilter()
    {
        FactionFilter.Items.Add("Все фракции");
        FactionFilter.Items.Add("Общие");
        FactionFilter.Items.Add(_vm.GetPlayer().Faction.ToString());
        FactionFilter.SelectedIndex = 0;
    }

    private void PopulateLevelFilter()
    {
        LevelFilter.Items.Add("Все ур.");
        for (int i = 1; i <= 10; i++)
            LevelFilter.Items.Add($"Ур. {i}");
        LevelFilter.SelectedIndex = 0;
    }

    private void PopulateNpcCombo()
    {
        NpcCombo.Items.Clear();
        foreach (var n in _vm.AliveNpcs)
            NpcCombo.Items.Add(n);
        if (NpcCombo.Items.Count > 0)
            NpcCombo.SelectedIndex = 0;
    }

    // ── Refresh ─────────────────────────────────────────────────────────────

    private void Refresh()
    {
        RefreshCatalog();
        RefreshInventory();
        RefreshNpcSection();
        StatusText.Text = $"ОР: {_vm.DevPoints:F0}  |  Терминал: {_vm.TerminalLevel}  |  В инвентаре: {_vm.TechInventory.Count}";
    }

    private void RefreshCatalog()
    {
        var all = _vm.GetTechniqueCatalog();
        int factionIdx = FactionFilter.SelectedIndex;
        int levelIdx   = LevelFilter.SelectedIndex;

        string playerFaction = _vm.GetPlayer().Faction.ToString();

        var filtered = all.AsEnumerable();

        if (factionIdx == 1)
            filtered = filtered.Where(t => t.Faction == "");
        else if (factionIdx == 2)
            filtered = filtered.Where(t => t.Faction == playerFaction || t.Faction == "");

        if (levelIdx > 0)
            filtered = filtered.Where(t => t.TerminalLevel == levelIdx);

        _catalogRows = filtered.OrderBy(t => t.TerminalLevel).ThenBy(t => t.Name)
                               .Select(t => new TechCatalogRow(t)).ToList();

        CatalogGrid.ItemsSource = null;
        CatalogGrid.ItemsSource = _catalogRows;
    }

    private void RefreshInventory()
    {
        var counts = _vm.TechInventoryCounts;
        var allTechs = _vm.GetTechniqueCatalog()
            .Where(t => counts.ContainsKey(t.CatalogKey))
            .OrderBy(t => t.Name)
            .Select(t => new TechInventoryRow(t, counts[t.CatalogKey]))
            .ToList();

        InventoryGrid.ItemsSource = null;
        InventoryGrid.ItemsSource = allTechs;

        TeachFromGrid.ItemsSource = null;
        TeachFromGrid.ItemsSource = allTechs;
    }

    private void RefreshNpcSection()
    {
        if (NpcCombo.SelectedItem is not Npc npc)
        {
            NpcTechGrid.ItemsSource = null;
            return;
        }

        var allTechs = _vm.GetTechniqueCatalog()
            .ToDictionary(t => t.CatalogKey);

        var rows = npc.LearnedTechIds
            .Where(k => allTechs.ContainsKey(k))
            .Select(k => new NpcTechRow(allTechs[k]))
            .ToList();

        NpcTechGrid.ItemsSource = null;
        NpcTechGrid.ItemsSource = rows;
    }

    // ── Filters ─────────────────────────────────────────────────────────────

    private void FactionFilter_Changed(object sender, SelectionChangedEventArgs e) => RefreshCatalog();
    private void LevelFilter_Changed(object sender, SelectionChangedEventArgs e) => RefreshCatalog();

    // ── Selection handlers ──────────────────────────────────────────────────

    private void CatalogGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CatalogGrid.SelectedItem is TechCatalogRow row)
            StatusText.Text = $"{row.Name} — {row.Description}  |  ОР: {row.OPCost:F0}  |  Терминал: {row.TerminalLevel}  |  Реж.: {row.ModesLabel}";
    }

    private void InventoryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (InventoryGrid.SelectedItem is TechInventoryRow row)
            StatusText.Text = $"{row.Name} — в инвентаре: {row.Count}";
    }

    private void TeachFromGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TeachFromGrid.SelectedItem is TechInventoryRow row)
            StatusText.Text = $"Выбрано для обучения: {row.Name}  |  Ур. {row.TerminalLevel}  |  В наличии: {row.Count}";
    }

    private void NpcCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshNpcSection();

    // ── Actions ─────────────────────────────────────────────────────────────

    private void BuyTech_Click(object sender, RoutedEventArgs e)
    {
        if (CatalogGrid.SelectedItem is not TechCatalogRow row) return;
        string result = _vm.BuyTechnique(row.Tech);
        _log(result, result.StartsWith("Куплено") ? "#56d364" : "#f87171");
        Refresh();
    }

    private void Teach_Click(object sender, RoutedEventArgs e)
    {
        if (NpcCombo.SelectedItem is not Npc npc) return;
        if (TeachFromGrid.SelectedItem is not TechInventoryRow row) return;

        string result = _vm.TeachTechnique(npc, row.Tech);
        _log(result, result.Contains("обучен") ? "#56d364" : "#f87171");
        Refresh();
        RefreshNpcComboKeepSelection();
    }

    private void RefreshNpcComboKeepSelection()
    {
        var prev = NpcCombo.SelectedItem as Npc;
        PopulateNpcCombo();
        if (prev != null)
        {
            for (int i = 0; i < NpcCombo.Items.Count; i++)
            {
                if (NpcCombo.Items[i] is Npc n && n.Id == prev.Id)
                {
                    NpcCombo.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    // ── Find NPCs ────────────────────────────────────────────────────────────

    private void FindNpcWith_Click(object sender, RoutedEventArgs e)
    {
        if (!GetSelectedTechKey(out string key, out string name)) return;

        var matches = _vm.AliveNpcs
            .Where(n => n.LearnedTechIds.Contains(key))
            .Select(n => n.Name)
            .ToList();

        string msg = matches.Count == 0
            ? $"Нет НПС с техникой «{name}»"
            : $"НПС с техникой «{name}»:\n" + string.Join("\n", matches);
        MessageBox.Show(msg, "Найти НПС", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void FindNpcWithout_Click(object sender, RoutedEventArgs e)
    {
        if (!GetSelectedTechKey(out string key, out string name)) return;

        var matches = _vm.AliveNpcs
            .Where(n => !n.LearnedTechIds.Contains(key))
            .Select(n => n.Name)
            .ToList();

        string msg = matches.Count == 0
            ? $"Все НПС знают технику «{name}»"
            : $"НПС без техники «{name}»:\n" + string.Join("\n", matches);
        MessageBox.Show(msg, "Найти НПС", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private bool GetSelectedTechKey(out string key, out string name)
    {
        key = "";
        name = "";

        if (TeachFromGrid.SelectedItem is TechInventoryRow invRow)
        {
            key = invRow.CatalogKey;
            name = invRow.Name;
            return true;
        }
        if (CatalogGrid.SelectedItem is TechCatalogRow catRow)
        {
            key = catRow.Tech.CatalogKey;
            name = catRow.Name;
            return true;
        }

        StatusText.Text = "Выберите технику в магазине или инвентаре";
        return false;
    }

    private void CatalogGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (CatalogGrid.SelectedItem is not TechCatalogRow row) return;
        var t = row.Tech;
        string reqs = t.RequiredStats.Count == 0 ? "нет" :
            string.Join(", ", t.RequiredStats.Select(kv => $"Стат {kv.Key} ≥ {kv.Value}"));
        string msg = $"{t.Name}\n\n{t.Description}\n\nТерминал: {t.TerminalLevel}  |  Тип: {t.TechType}  |  ОР: {t.OPCost:F0}\n" +
                     $"Требования: {reqs}\n" +
                     $"Режимов активации: {t.ActivationModes.Count}  |  Лечение: {(t.HealAmount > 0 ? t.HealAmount.ToString("F0") : "—")}";
        MessageBox.Show(msg, t.Name, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
