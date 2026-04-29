using System.Windows;
using System.Windows.Controls;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;
using ApocMinimal.Models.UIData;
using ApocMinimal.Models.LocationData;
using ApocMinimal.Systems;
using ApocMinimal.ViewModels;
using System.Windows.Media;
using ApocMinimal.Models.TechniqueData;

namespace ApocMinimal;

public partial class AltarWindow : Window
{
    private readonly GameViewModel _vm;
    ListPlayerFactions listPlayerFactions;

    public AltarWindow(GameViewModel vm)
    {
        listPlayerFactions = new ListPlayerFactions();
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
        TechPanel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "Управление техниками — открыть через кнопку «Техники».",
            Foreground = System.Windows.Media.Brushes.Gray,
            Margin = new System.Windows.Thickness(8),
        });
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
        BarrierInfoLabel.Text =
            $"Уровень барьера: {_vm.BarrierLevel}  |  ОР: {_vm.DevPoints:F0}\n" +
            $"Базовые единицы: {_vm.BaseUnits}  |  Защищено зон: {_vm.ControlledZoneIds.Count}";

        BarrierUpgradeBtn.IsEnabled = _vm.DevPoints >= 20;
        BarrierHintLabel.Text = "Квартира: 5 ОР  |  Этаж: 10 ОР  |  Здание: 20 ОР  |  Улица: 50 ОР  (только зачищенные)";

        BarrierMapPanel.Children.Clear();

        var allLocs   = _vm.Locations;
        var protected_ = _vm.ControlledZoneIds;

        // Only explored + cleared locations are protectable candidates
        bool IsCandidate(Location l) =>
            l.IsExplored && l.Status == LocationStatus.Cleared &&
            (l.Type == LocationType.Street || l.Type == LocationType.Building ||
             l.Type == LocationType.Floor  || l.Type == LocationType.Apartment);

        // Adjacency: free if first; otherwise parent or sibling is protected
        bool IsAdjacent(Location loc)
        {
            if (protected_.Count == 0) return true;
            if (protected_.Contains(loc.ParentId)) return true;
            return allLocs.Any(l => l.ParentId == loc.ParentId && protected_.Contains(l.Id));
        }

        var streets = allLocs
            .Where(l => l.Type == LocationType.Street && l.IsExplored)
            .OrderBy(l => l.Name)
            .ToList();

        if (streets.Count == 0 && !allLocs.Any(IsCandidate))
        {
            BarrierMapPanel.Children.Add(new TextBlock
            {
                Text = "Нет зачищенных исследованных локаций.",
                Foreground = MakeBrush("#8b949e"), FontSize = 10,
            });
            return;
        }

        foreach (var street in streets)
        {
            // Street header
            bool streetProtected  = protected_.Contains(street.Id);
            bool streetIsCandidate = IsCandidate(street);
            bool streetAdjacent   = IsAdjacent(street);

            AddBarrierRow(BarrierMapPanel, street, 0, "🏙", "#e3b341",
                streetProtected, streetIsCandidate, streetAdjacent);

            var buildings = allLocs
                .Where(l => l.Type == LocationType.Building && l.ParentId == street.Id && l.IsExplored)
                .OrderBy(l => l.Name)
                .ToList();

            foreach (var building in buildings)
            {
                bool bProtected   = protected_.Contains(building.Id);
                bool bCandidate   = IsCandidate(building);
                bool bAdjacent    = IsAdjacent(building);

                AddBarrierRow(BarrierMapPanel, building, 12, "🏢", "#79c0ff",
                    bProtected, bCandidate, bAdjacent);

                var floors = allLocs
                    .Where(l => l.Type == LocationType.Floor && l.ParentId == building.Id && l.IsExplored)
                    .OrderBy(l => l.Name)
                    .ToList();

                foreach (var floor in floors)
                {
                    bool fProtected = protected_.Contains(floor.Id);
                    bool fCandidate = IsCandidate(floor);
                    bool fAdjacent  = IsAdjacent(floor);

                    AddBarrierRow(BarrierMapPanel, floor, 24, "📋", "#8b949e",
                        fProtected, fCandidate, fAdjacent);

                    var apts = allLocs
                        .Where(l => l.Type == LocationType.Apartment && l.ParentId == floor.Id && l.IsExplored)
                        .OrderBy(l => l.Name)
                        .ToList();

                    foreach (var apt in apts)
                    {
                        bool aProtected = protected_.Contains(apt.Id);
                        bool aCandidate = IsCandidate(apt);
                        bool aAdjacent  = IsAdjacent(apt);

                        AddBarrierRow(BarrierMapPanel, apt, 36, "🚪", "#c9d1d9",
                            aProtected, aCandidate, aAdjacent);
                    }
                }
            }
        }

        // Locations without a street parent (fallback)
        var orphans = allLocs
            .Where(l => IsCandidate(l) &&
                        l.Type != LocationType.Street &&
                        !streets.Any(s => allLocs.Any(b => b.Id == l.ParentId && b.ParentId == s.Id) ||
                                          l.ParentId == s.Id))
            .OrderBy(l => l.Name)
            .ToList();
        foreach (var loc in orphans)
        {
            bool isProtected = protected_.Contains(loc.Id);
            AddBarrierRow(BarrierMapPanel, loc, 0, "📌", "#8b949e",
                isProtected, true, IsAdjacent(loc));
        }
    }

    private void AddBarrierRow(
        Panel panel, Location loc,
        double indent, string icon, string iconColor,
        bool isProtected, bool isCandidate, bool isAdjacent)
    {
        double cost = loc.Type switch
        {
            LocationType.Apartment => 5,
            LocationType.Floor     => 10,
            LocationType.Building  => 20,
            LocationType.Street    => 50,
            _                      => 100,
        };

        bool canAfford   = _vm.DevPoints >= cost;
        bool canProtect  = isCandidate && isAdjacent && !isProtected && canAfford;
        bool showProtect = isCandidate && !isProtected;

        string nameColor = isProtected ? "#56d364"
            : (!isCandidate  ? "#3a3a3a"
            : (!isAdjacent   ? "#4b5563"
            : "#c9d1d9"));

        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(indent, 1, 0, 1) };

        // Icon
        row.Children.Add(new TextBlock
        {
            Text      = icon,
            FontSize  = 10,
            Margin    = new Thickness(0, 0, 4, 0),
            Foreground = MakeBrush(iconColor),
            VerticalAlignment = VerticalAlignment.Center,
        });

        // Name
        string lockHint = !isCandidate  ? " [опасно]"
                        : !isAdjacent   ? " [не рядом]"
                        : "";
        row.Children.Add(new TextBlock
        {
            Text      = $"{loc.Name}{lockHint}",
            Foreground = MakeBrush(nameColor),
            FontSize  = 10,
            Width     = Math.Max(180 - indent, 80),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = System.Windows.TextTrimming.CharacterEllipsis,
        });

        if (isProtected)
        {
            var btn = new Button
            {
                Content    = "Снять",
                Style      = (Style)FindResource("ABtn"),
                Background = MakeBrush("#1a2a1a"),
                Foreground = MakeBrush("#f87171"),
                Width = 54, Height = 20, FontSize = 9,
                Tag = loc.Id,
            };
            btn.Click += UnprotectBtn_Click;
            row.Children.Add(btn);
        }
        else if (showProtect)
        {
            var btn = new Button
            {
                Content   = $"+{cost:F0} ОР",
                Style     = (Style)FindResource("ABtn"),
                IsEnabled = canProtect,
                Width = 60, Height = 20, FontSize = 9,
                Tag = loc.Id,
                ToolTip = !isAdjacent ? "Сначала защитите соседнюю локацию"
                         : !canAfford ? $"Нужно {cost:F0} ОР"
                         : $"Защитить за {cost:F0} ОР",
            };
            btn.Click += ProtectBtn_Click;
            row.Children.Add(btn);
        }

        panel.Children.Add(row);
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
        OnePlayerFaction onePlayerFaction = listPlayerFactions.factions.FirstOrDefault(pf => pf.Faction == faction);
        TerminalAbilPanel.Children.Add(new TextBlock
        {
            Text = $"Фракция: {onePlayerFaction.Label}",
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
