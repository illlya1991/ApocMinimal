using System.Windows;
using System.Windows.Controls;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;
using ApocMinimal.Models.TechniqueData;
using ApocMinimal.Models.UIData;
using ApocMinimal.Systems;
using ApocMinimal.ViewModels;
using System.Windows.Media;

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
        RefreshTerminalAbilTab();
        RefreshTechTab();
    }

    private void RefreshAltarTab()
    {
        AltarInfoLabel.Text =
            $"Уровень Терминала: {_vm.TerminalLevel} / 10\n" +
            $"ОР: {_vm.DevPoints:F0}\n" +
            $"Стоимость улучшения: {_vm.UpgradeCost:N0} ОР";

        UpgradeAltarBtn.IsEnabled = _vm.CanUpgrade;
        UpgradeAltarBtn.Content = _vm.TerminalLevel >= 10
            ? "Максимальный уровень"
            : $"Улучшить ({_vm.UpgradeCost:N0} ОР)";

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
            bool unlocked = tech.TerminalLevel <= _vm.TerminalLevel;
            var btn = new Button
            {
                Content = $"{tech.Name}  ({tech.OPCost:F0} ОР)",
                Style = (Style)FindResource("ABtn"),
                IsEnabled = unlocked && _vm.DevPoints >= tech.OPCost,
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
        _vm.DevPoints -= tech.OPCost;
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
        _vm.DevPoints -= _vm.UpgradeCost;
        _vm.TerminalLevel++;
        _vm.SavePlayer();
        _vm.Refresh();
        Refresh();
    }

    private void RefreshBarrierTab()
    {
        int units = _vm.BaseUnits;
        int protectedCount = _vm.ControlledZoneIds.Count;
        BarrierInfoLabel.Text =
            $"Уровень барьера: {_vm.BarrierLevel}  |  ОР: {_vm.DevPoints:F0}\n" +
            $"Базовые единицы: {units}  |  Защищено зон: {protectedCount}\n" +
            $"Размер барьера: {_vm.BarrierSize:F0} м";

        BarrierUpgradeBtn.IsEnabled = _vm.DevPoints >= 20;

        BarrierHintLabel.Text = "Квартира: 5 ОР  |  Этаж: 10 ОР  |  Здание: 20 ОР  |  Улица: 50 ОР";

        BarrierMapPanel.Children.Clear();

        var exploredLocs = _vm.Locations
            .Where(l => l.IsExplored &&
                        (l.Type == ApocMinimal.Models.LocationData.LocationType.Apartment ||
                         l.Type == ApocMinimal.Models.LocationData.LocationType.Floor ||
                         l.Type == ApocMinimal.Models.LocationData.LocationType.Building ||
                         l.Type == ApocMinimal.Models.LocationData.LocationType.Street))
            .OrderBy(l => (int)l.Type)
            .ThenBy(l => l.Name)
            .ToList();

        if (exploredLocs.Count == 0)
        {
            BarrierMapPanel.Children.Add(new TextBlock
            {
                Text = "Нет исследованных локаций.",
                Foreground = MakeBrush("#8b949e"),
                FontSize = 10,
            });
            return;
        }

        foreach (var loc in exploredLocs)
        {
            bool isProtected = _vm.ControlledZoneIds.Contains(loc.Id);
            double cost = loc.Type switch
            {
                ApocMinimal.Models.LocationData.LocationType.Apartment => 5,
                ApocMinimal.Models.LocationData.LocationType.Floor => 10,
                ApocMinimal.Models.LocationData.LocationType.Building => 20,
                ApocMinimal.Models.LocationData.LocationType.Street => 50,
                _ => 100,
            };

            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };

            string label = $"[{loc.TypeLabel}]  {loc.Name}";
            row.Children.Add(new TextBlock
            {
                Text = label,
                Foreground = isProtected ? MakeBrush("#56d364") : MakeBrush("#c9d1d9"),
                FontSize = 10,
                Width = 260,
                VerticalAlignment = VerticalAlignment.Center,
            });

            if (isProtected)
            {
                var unprotectBtn = new Button
                {
                    Content = "Снять",
                    Style = (Style)FindResource("ABtn"),
                    Background = MakeBrush("#1a2a1a"),
                    Foreground = MakeBrush("#f87171"),
                    Width = 60,
                    Height = 22,
                    FontSize = 10,
                    Tag = loc.Id,
                };
                unprotectBtn.Click += UnprotectBtn_Click;
                row.Children.Add(unprotectBtn);
            }
            else
            {
                var protectBtn = new Button
                {
                    Content = $"Защитить ({cost:F0} ОР)",
                    Style = (Style)FindResource("ABtn"),
                    IsEnabled = _vm.DevPoints >= cost,
                    Width = 110,
                    Height = 22,
                    FontSize = 10,
                    Tag = loc.Id,
                };
                protectBtn.Click += ProtectBtn_Click;
                row.Children.Add(protectBtn);
            }

            BarrierMapPanel.Children.Add(row);
        }
    }

    private void ProtectBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not int id) return;
        _vm.ProtectLocation(id);
        RefreshBarrierTab();
    }

    private void UnprotectBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not int id) return;
        _vm.UnprotectLocation(id);
        RefreshBarrierTab();
    }

    private void BarrierUpgradeBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.DevPoints < 20) return;
        _vm.DevPoints -= 20;
        _vm.BarrierLevel++;
        _vm.BarrierSize = _vm.BarrierLevel * 50;
        _vm.SavePlayer();
        _vm.Refresh();
        RefreshBarrierTab();
    }

    // =========================================================
    // Способности Терминала
    // =========================================================

    private void RefreshTerminalAbilTab()
    {
        TerminalAbilPanel.Children.Clear();

        TerminalAbilPanel.Children.Add(new TextBlock
        {
            Text = $"Уровень Терминала: {_vm.TerminalLevel} / 10",
            Foreground = MakeBrush("#58a6ff"),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4),
        });

        // Show current faction
        var faction = _vm.PlayerFaction;
        TerminalAbilPanel.Children.Add(new TextBlock
        {
            Text = $"Фракция: {faction.ToLabel()}",
            Foreground = MakeBrush("#f59e0b"),
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 8),
        });

        int[] levels = { 2, 4, 6, 8, 10 };
        foreach (int lvl in levels)
        {
            bool unlocked = _vm.TerminalLevel >= lvl;
            string headerColor = unlocked ? "#f59e0b" : "#4b5563";

            TerminalAbilPanel.Children.Add(new TextBlock
            {
                Text = $"── УРОВЕНЬ {lvl} ──────────────────",
                Foreground = MakeBrush(headerColor),
                FontSize = 10,
                Margin = new Thickness(0, 8, 0, 4),
            });

            foreach (var abil in TerminalAbilityCatalog.GetForLevel(lvl))
            {
                string nameColor = unlocked
                    ? abil.AbilityType switch
                    {
                        TerminalAbilityType.Base   => "#79c0ff",
                        TerminalAbilityType.Earth  => "#f59e0b",
                        TerminalAbilityType.Unique => "#f87171",
                        _ => "#c9d1d9"
                    }
                    : "#4b5563";

                var row = new StackPanel { Margin = new Thickness(0, 2, 0, 2) };

                row.Children.Add(new TextBlock
                {
                    Text = $"{abil.TypeIcon} [{abil.TypeLabel}] {abil.Name}" +
                           (abil.OPCostPerUse > 0 ? $"  ({abil.OPCostPerUse:F0} ОР)" : "  [Пассив]"),
                    Foreground = MakeBrush(nameColor),
                    FontSize = 11,
                    FontWeight = unlocked ? FontWeights.SemiBold : FontWeights.Normal,
                    Opacity = unlocked ? 1.0 : 0.4,
                });

                row.Children.Add(new TextBlock
                {
                    Text = "   " + abil.Description,
                    Foreground = MakeBrush(unlocked ? "#8b949e" : "#4b5563"),
                    FontSize = 10,
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = unlocked ? 1.0 : 0.35,
                    Margin = new Thickness(0, 1, 0, 4),
                });
            }

            if (!unlocked)
            {
                TerminalAbilPanel.Children.Add(new TextBlock
                {
                    Text = $"   [Заблокировано до уровня Терминала {lvl}]",
                    Foreground = MakeBrush("#4b5563"),
                    FontSize = 10,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 0, 0, 4),
                });
            }
        }

        // ── Фракционные способности ──────────────────────────────────────
        TerminalAbilPanel.Children.Add(new TextBlock
        {
            Text = "── СПОСОБНОСТИ ФРАКЦИИ ────────────────",
            Foreground = MakeBrush("#f59e0b"),
            FontSize = 10,
            Margin = new Thickness(0, 14, 0, 4),
        });

        foreach (int lvl in levels)
        {
            bool unlocked = _vm.TerminalLevel >= lvl;
            var fAbil = FactionAbilityCatalog.All
                .FirstOrDefault(a => a.Faction == faction && a.UnlockLevel == lvl);
            if (fAbil == null) continue;

            string nameColor = unlocked ? "#c084fc" : "#4b5563";
            var row = new StackPanel { Margin = new Thickness(0, 2, 0, 2) };

            row.Children.Add(new TextBlock
            {
                Text = $"[Ур.{lvl}] {fAbil.Name}" +
                       (fAbil.OPCostPerUse > 0 ? $"  ({fAbil.OPCostPerUse:F0} ОР)" : "  [Пассив]"),
                Foreground = MakeBrush(nameColor),
                FontSize = 11,
                FontWeight = unlocked ? FontWeights.SemiBold : FontWeights.Normal,
                Opacity = unlocked ? 1.0 : 0.4,
            });
            row.Children.Add(new TextBlock
            {
                Text = "   " + fAbil.Description,
                Foreground = MakeBrush(unlocked ? "#8b949e" : "#4b5563"),
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Opacity = unlocked ? 1.0 : 0.35,
                Margin = new Thickness(0, 1, 0, 4),
            });
            TerminalAbilPanel.Children.Add(row);
        }
    }

    // =========================================================
    // Техники / Способности
    // =========================================================

    private void RefreshTechTab()
    {
        // Repopulate NPC combo (preserve selection if possible)
        var prevNpc = TeachNpcCombo.SelectedItem as Npc;
        TeachNpcCombo.SelectionChanged -= TeachNpcCombo_SelectionChanged;
        TeachNpcCombo.Items.Clear();
        foreach (var n in _vm.AliveNpcs)
            TeachNpcCombo.Items.Add(n);
        if (prevNpc != null)
        {
            var match = _vm.AliveNpcs.FirstOrDefault(n => n.Id == prevNpc.Id);
            if (match != null) TeachNpcCombo.SelectedItem = match;
        }
        TeachNpcCombo.SelectionChanged += TeachNpcCombo_SelectionChanged;

        TechShopPanel.Children.Clear();

        // ── Shop: Techniques ──────────────────────────────────────────────
        TechShopPanel.Children.Add(MakeHdr("ТЕХНИКИ"));
        foreach (var tech in TechAbilityCatalog.Techniques.OrderBy(t => t.TerminalLevel).ThenBy(t => t.Name))
        {
            bool unlocked = _vm.TerminalLevel >= tech.TerminalLevel;
            string kindLabel = tech.Kind == TechKind.Passive ? "[П]" : "[А]";
            string tooltip = tech.Kind == TechKind.Passive
                ? tech.Description
                : $"{tech.Description}\n⚔ {tech.CombatEffect}\n🌿 {tech.LifeEffect}";

            var btn = new Button
            {
                Content = $"ур.{tech.TerminalLevel}  {kindLabel}  {tech.Name}  ({tech.BuyCost:F0} ОР)",
                Style = (Style)FindResource("ABtn"),
                IsEnabled = unlocked && _vm.DevPoints >= tech.BuyCost,
                Opacity = unlocked ? 1.0 : 0.45,
                ToolTip = tooltip,
                Tag = tech.Id,
            };
            if (unlocked) btn.Click += TechShopBuy_Click;
            TechShopPanel.Children.Add(btn);
        }

        // ── Shop: Abilities ───────────────────────────────────────────────
        TechShopPanel.Children.Add(MakeHdr("СПОСОБНОСТИ"));
        foreach (var abil in TechAbilityCatalog.Abilities.OrderBy(a => a.TerminalLevel).ThenBy(a => a.Name))
        {
            bool unlocked = _vm.TerminalLevel >= abil.TerminalLevel;
            var techNames = abil.TechniqueIds
                .Select(id => TechAbilityCatalog.FindTech(id)?.Name ?? id)
                .ToList();
            string tooltip = $"{abil.Description}\nВключает: {string.Join(", ", techNames)}";

            var btn = new Button
            {
                Content = $"ур.{abil.TerminalLevel}  [Сп]  {abil.Name}  ({abil.BuyCost:F0} ОР)",
                Style = (Style)FindResource("ABtn"),
                Background = MakeBrush("#1a1a2e"),
                Foreground = MakeBrush("#c084fc"),
                IsEnabled = unlocked && _vm.DevPoints >= abil.BuyCost,
                Opacity = unlocked ? 1.0 : 0.45,
                ToolTip = tooltip,
                Tag = abil.Id,
            };
            if (unlocked) btn.Click += AbilShopBuy_Click;
            TechShopPanel.Children.Add(btn);
        }

        // ── Inventory panel ───────────────────────────────────────────────
        RefreshTechInventoryPanel();
    }

    private void RefreshTechInventoryPanel()
    {
        TechInventoryPanel.Children.Clear();

        var inv = _vm.TechInventory;
        if (inv.Count == 0)
        {
            TechInventoryPanel.Children.Add(new TextBlock
            {
                Text = "Инвентарь пуст.",
                Foreground = MakeBrush("#8b949e"),
                FontSize = 10,
                Margin = new Thickness(0, 2, 0, 2),
            });
            return;
        }

        // Count how many of each item
        var grouped = inv
            .GroupBy(e => (e.ItemId, e.ItemType))
            .Select(g => new { g.Key.ItemId, g.Key.ItemType, Count = g.Count(), FirstRowId = g.First().RowId })
            .ToList();

        Npc? selectedNpc = TeachNpcCombo.SelectedItem as Npc;

        foreach (var item in grouped)
        {
            string name;
            string tooltip;
            string fgColor = "#c9d1d9";

            if (item.ItemType == "Technique")
            {
                var def = TechAbilityCatalog.FindTech(item.ItemId);
                name = def?.Name ?? item.ItemId;
                string kindLabel = def?.Kind == TechKind.Passive ? "[Пассив]" : "[Актив]";
                tooltip = def == null ? "" :
                    (def.Kind == TechKind.Passive
                        ? def.Description
                        : $"{def.Description}\n⚔ {def.CombatEffect}\n🌿 {def.LifeEffect}");
                fgColor = def?.Kind == TechKind.Passive ? "#7ee787" : "#79c0ff";
            }
            else
            {
                var def = TechAbilityCatalog.FindAbility(item.ItemId);
                name = def?.Name ?? item.ItemId;
                var techNames = def?.TechniqueIds
                    .Select(id => TechAbilityCatalog.FindTech(id)?.Name ?? id) ?? Enumerable.Empty<string>();
                tooltip = def == null ? "" : $"{def.Description}\nВключает: {string.Join(", ", techNames)}";
                fgColor = "#c084fc";
            }

            string countStr = item.Count > 1 ? $" ×{item.Count}" : "";
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };

            row.Children.Add(new TextBlock
            {
                Text = $"  {name}{countStr}",
                Foreground = MakeBrush(fgColor),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 200,
                ToolTip = tooltip,
            });

            var teachBtn = new Button
            {
                Content = "Обучить",
                Style = (Style)FindResource("ABtn"),
                Width = 70,
                Height = 22,
                FontSize = 10,
                IsEnabled = selectedNpc != null,
                Tag = (item.FirstRowId, item.ItemId, item.ItemType),
            };
            teachBtn.Click += TeachBtn_Click;
            row.Children.Add(teachBtn);

            TechInventoryPanel.Children.Add(row);
        }
    }

    private void TechShopBuy_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string id) return;
        _vm.BuyTechItem(id, "Technique");
        _vm.Refresh();
        RefreshAltarTab();
        RefreshTechTab();
    }

    private void AbilShopBuy_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string id) return;
        _vm.BuyTechItem(id, "Ability");
        _vm.Refresh();
        RefreshAltarTab();
        RefreshTechTab();
    }

    private void TeachNpcCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshTechInventoryPanel();
    }

    private void TeachBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.Tag is not (int rowId, string itemId, string itemType)) return;
        if (TeachNpcCombo.SelectedItem is not Npc npc) return;

        string result = _vm.TeachTechItem(npc, rowId, itemId, itemType);
        _vm.Refresh();
        RefreshTechTab();
    }

    private TextBlock MakeHdr(string text) => new TextBlock
    {
        Text = text,
        FontSize = 10,
        Foreground = MakeBrush("#8b949e"),
        Margin = new Thickness(0, 8, 0, 4),
    };

    private static System.Windows.Media.SolidColorBrush MakeBrush(string hex) =>
        (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString(hex)!;

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
                Text = "▼ ОТДАНО:",
                Foreground = MakeBrush("#f87171"),
                FontSize = 9,
                Margin = new Thickness(0, 4, 0, 1),
            });
            sp.Children.Add(new TextBlock
            {
                Text = "   " + ex.GiveText,
                Foreground = MakeBrush("#fca5a5"),
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
            });
            sp.Children.Add(new TextBlock
            {
                Text = "▲ ПОЛУЧЕНО:",
                Foreground = MakeBrush("#56d364"),
                FontSize = 9,
                Margin = new Thickness(0, 4, 0, 1),
            });
            sp.Children.Add(new TextBlock
            {
                Text = "   " + ex.GetText,
                Foreground = MakeBrush("#86efac"),
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
                (_vm.DevPoints >= 5 && _vm.Resources.Any(r => r.Name == e.Name && r.Amount >= 1))
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
                        Content = $"{entry.Name} — Разблокировать (1 ед. + 5 ОР)",
                        Style = (Style)FindResource("ABtn"),
                        IsEnabled = _vm.DevPoints >= 5 &&
                                    _vm.Resources.Any(r => r.Name == entry.Name && r.Amount >= 1),
                        Tag = entry.Name,
                    };
                    btn.Click += ShopUnlock_Click;
                }
                else
                {
                    btn = new Button
                    {
                        Content = $"{entry.Name} ×10 → {price:F0} ОР",
                        Style = (Style)FindResource("ABtn"),
                        Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#1a2a1a")!,
                        Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#56d364")!,
                        IsEnabled = _vm.DevPoints >= price,
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
