using System.Windows;
using System.Windows.Controls;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;
using ApocMinimal.Models.UIData;
using ApocMinimal.Systems;
using ApocMinimal.ViewModels;

namespace ApocMinimal;

public partial class AltarWindow : Window
{
    private readonly GameViewModel _vm;

    public AltarWindow(GameViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        Refresh();
    }

    private void Refresh()
    {
        RefreshAltarTab();
        RefreshBarrierTab();
        RefreshExchangesTab();
        RefreshShopTab();
    }

    private void RefreshAltarTab()
    {
        AltarInfoLabel.Text =
            $"Уровень: {_vm.AltarLevel} / 10\n" +
            $"ОВ: {_vm.FaithPoints:F0}\n" +
            $"Стоимость улучшения: {_vm.UpgradeCost:N0} ОВ";

        UpgradeAltarBtn.IsEnabled = _vm.CanUpgrade;
        UpgradeAltarBtn.Content = _vm.AltarLevel >= 10
            ? "Максимальный уровень"
            : $"Улучшить ({_vm.UpgradeCost:N0} ОВ)";

        var sb = new System.Text.StringBuilder();
        for (int fl = 1; fl <= 5; fl++)
        {
            int lim = _vm.GetFollowerLimit(fl);
            if (lim == 0) continue;
            int cur = _vm.GetFollowerCountAtLevel(fl);
            sb.AppendLine($"Уровень {fl}: {cur}/{(lim == -1 ? "∞" : lim.ToString())}");
        }
        FollowerLabel.Text = sb.ToString().TrimEnd();

        TechPanel.Children.Clear();
        foreach (var tech in _vm.AllTechniques)
        {
            bool unlocked = tech.AltarLevel <= _vm.AltarLevel;
            var btn = new Button
            {
                Content = $"{tech.Name}  ({tech.FaithCost:F0} ОВ)",
                Style = (Style)FindResource("ABtn"),
                IsEnabled = unlocked && _vm.FaithPoints >= tech.FaithCost,
                Opacity = unlocked ? 1.0 : 0.4,
                ToolTip = tech.Description,
                Tag = tech,
            };
            if (unlocked) btn.Click += TechBtn_Click;
            TechPanel.Children.Add(btn);
        }
    }

    private void TechBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not Technique tech) return;
        _vm.FaithPoints -= tech.FaithCost;
        _vm.SavePlayer();
        var target = tech.HealAmount > 0
            ? _vm.AliveNpcs.OrderBy(n => n.Health).FirstOrDefault()
            : _vm.AliveNpcs.OrderByDescending(n => n.Initiative).FirstOrDefault();
        if (target == null) return;
        if (TechniqueSystem.Apply(tech, target, out string log))
            _vm.SaveNpc(target);
        _vm.Refresh();
        Refresh();
    }

    private void UpgradeAltarBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!_vm.CanUpgrade) return;
        _vm.FaithPoints -= _vm.UpgradeCost;
        _vm.AltarLevel++;
        _vm.SavePlayer();
        _vm.Refresh();
        Refresh();
    }

    private void RefreshBarrierTab()
    {
        int units = _vm.BaseUnits;
        BarrierInfoLabel.Text =
            $"Уровень барьера: {_vm.BarrierLevel}\n" +
            $"Базовые единицы: {units}\n" +
            $"Формула: Алтарь({_vm.AltarLevel}) × Барьер({_vm.BarrierLevel}) × (Барьер+1) / 2\n" +
            $"Размер барьера: {_vm.BarrierSize:F0} м";

        BarrierUpgradeBtn.IsEnabled = _vm.FaithPoints >= 20;

        var zones = _vm.ControlledZoneIds;
        ControlledZonesLabel.Text = zones.Count == 0
            ? "Нет защищённых зон."
            : string.Join(", ", zones.Select(id =>
            {
                var loc = _vm.Locations.FirstOrDefault(l => l.Id == id);
                return loc != null ? $"{loc.Name}" : $"Зона #{id}";
            }));
    }

    private void BarrierUpgradeBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.FaithPoints < 20) return;
        _vm.FaithPoints -= 20;
        _vm.BarrierLevel++;
        _vm.BarrierSize = _vm.BarrierLevel * 50;
        _vm.SavePlayer();
        _vm.Refresh();
        RefreshBarrierTab();
    }

    private void RefreshExchangesTab()
    {
        ExchangeListPanel.Children.Clear();
        var applied = _vm.AppliedExchangesList;
        ExchangeEmptyLabel.Visibility = applied.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        foreach (var ex in applied)
        {
            var border = new Border
            {
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#0d1f0d")!,
                BorderBrush = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#f59e0b")!,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 2, 0, 4),
                Padding = new Thickness(6, 4, 6, 4),
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = ex.Name,
                Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#f59e0b")!,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
            });
            sp.Children.Add(new TextBlock
            {
                Text = "✖ " + ex.GiveText,
                Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#f87171")!,
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
            });
            sp.Children.Add(new TextBlock
            {
                Text = "✔ " + ex.GetText,
                Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#56d364")!,
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
            });
            border.Child = sp;
            ExchangeListPanel.Children.Add(border);
        }
    }

    private void RefreshShopTab()
    {
        ShopPanel.Children.Clear();
        var resources = _vm.GetShoppableResources();
        var filtered = resources
            .Where(e => e.IsLocationNode && (
                _vm.IsShopUnlocked(e.Name) ||
                (_vm.FaithPoints >= 5 && _vm.Resources.Any(r => r.Name == e.Name && r.Amount >= 1))
            ))
            .GroupBy(e => e.Quality)
            .OrderBy(g => g.Key);

        foreach (var group in filtered)
        {
            string catName = group.Key switch
            {
                1 => "★ Базовые",
                2 => "★★ Стандартные",
                3 => "★★★ Редкие",
                4 => "★★★★ Эпические",
                5 => "★★★★★ Уникальные",
                _ => "Прочее"
            };
            ShopPanel.Children.Add(new TextBlock
            {
                Text = catName,
                Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#8b949e")!,
                FontSize = 10,
                Margin = new Thickness(0, 6, 0, 2),
            });

            foreach (var entry in group.OrderBy(e => e.Name))
            {
                bool unlocked = _vm.IsShopUnlocked(entry.Name);
                double price = entry.Quality switch { 1=>2,2=>3,3=>5,4=>10,5=>20,_=>5 };
                Button btn;
                if (!unlocked)
                {
                    btn = new Button
                    {
                        Content = $"{entry.Name} — Разблокировать (1 ед. + 5 ОВ)",
                        Style = (Style)FindResource("ABtn"),
                        IsEnabled = _vm.FaithPoints >= 5 &&
                                    _vm.Resources.Any(r => r.Name == entry.Name && r.Amount >= 1),
                        Tag = entry.Name,
                    };
                    btn.Click += ShopUnlock_Click;
                }
                else
                {
                    btn = new Button
                    {
                        Content = $"{entry.Name} ×10 → {price:F0} ОВ",
                        Style = (Style)FindResource("ABtn"),
                        Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#1a2a1a")!,
                        Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#56d364")!,
                        IsEnabled = _vm.FaithPoints >= price,
                        Tag = entry.Name,
                    };
                    btn.Click += ShopBuy_Click;
                }
                ShopPanel.Children.Add(btn);
            }
        }
    }

    private void ShopUnlock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string name) return;
        _vm.UnlockShopResource(name);
        _vm.Refresh();
        RefreshShopTab();
        RefreshAltarTab();
    }

    private void ShopBuy_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string name) return;
        _vm.BuyShopResource(name);
        _vm.Refresh();
        RefreshShopTab();
        RefreshAltarTab();
    }
}
