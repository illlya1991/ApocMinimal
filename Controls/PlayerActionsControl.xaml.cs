using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ApocMinimal.Models.GameActions;
using ApocMinimal.Models.LocationData;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;
using ApocMinimal.Models.UIData;
using ApocMinimal.Systems;
using ApocMinimal.ViewModels;
using ApocMinimal.Services;

namespace ApocMinimal.Controls;

public partial class PlayerActionsControl : UserControl
{
    private GameViewModel _viewModel;
    private readonly GameUIService _uiService;
    private readonly List<ComboBox> _dynamicComboBoxes = new();

    public event Action<string, string>? LogAction;

    // Конструктор без параметров для XAML
    public PlayerActionsControl()
    {
        InitializeComponent();
        _viewModel = null!;
        _uiService = new GameUIService(Log);
    }

    public PlayerActionsControl(GameViewModel viewModel) : this()
    {
        _viewModel = viewModel;
        InitializeUI();
    }

    public void SetViewModel(GameViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeUI();
    }

    private void Log(string text, string color)
    {
        LogAction?.Invoke(text, color);
    }

    private void InitializeUI()
    {
        ActionCombo.Items.Clear();

        foreach (var group in _viewModel.ActionGroups)
            ActionCombo.Items.Add(group);

        ActionCombo.SelectedIndex = -1;

        SubActionRow.Visibility = Visibility.Collapsed;
        ParametersPanel.Visibility = Visibility.Collapsed;

        RefreshMapTab();
        RefreshTechniquePanel();
        RefreshResourceCombo();
    }

    public void Refresh()
    {
        RefreshMapTab();
        RefreshTechniquePanel();
        RefreshResourceCombo();
        RefreshActionCombo();
        RefreshExecuteBtn();
    }

    private void RefreshExecuteBtn()
    {
        if (ExecuteBtn == null || _viewModel == null) return;
        bool noActionsLeft = !_viewModel.HasActionsLeft;
        bool consumesAction = _viewModel.SelectedAction?.ConsumesAction ?? true;
        bool blocked = noActionsLeft && consumesAction;
        ExecuteBtn.IsEnabled = !blocked;
        ExecuteBtn.Opacity = blocked ? 0.4 : 1.0;
        ExecuteBtn.ToolTip = blocked
            ? $"Все действия за день использованы (лимит {Player.MaxPlayerActionsPerDay})"
            : null;
    }

    private void RefreshActionCombo()
    {
        if (ActionCombo.SelectedItem is PlayerActionGroup group)
        {
            var actions = _viewModel.GetActionsByGroup(group.Id);
            SubActionCombo.Items.Clear();
            foreach (var action in actions)
                SubActionCombo.Items.Add(action);
        }
    }

    private void RefreshResourceCombo()
    {
        // Для обратной совместимости
    }

    private void RefreshMapTab()
    {
        MapPanel.Children.Clear();
        var locs = _viewModel.Locations;
        var controlled = _viewModel.ControlledZoneIds;

        foreach (var city in locs.Where(l => l.Type == LocationType.City))
        {
            MapPanel.Children.Add(MapRow("🌆 " + city.Name, 0, "#58a6ff", "#0d1f35", controlled.Contains(city.Id)));

            foreach (var district in locs.Where(l => l.ParentId == city.Id).OrderBy(l => l.Name))
            {
                var distStreets = locs.Where(l => l.ParentId == district.Id).ToList();
                int totalBld = distStreets.SelectMany(s => locs.Where(l => l.ParentId == s.Id && l.Type == LocationType.Building)).Count();
                int expBld = distStreets.SelectMany(s => locs.Where(l => l.ParentId == s.Id && l.Type == LocationType.Building && l.IsExplored)).Count();

                if (!district.IsExplored)
                {
                    MapPanel.Children.Add(MapRow($"  ? Район: {district.Name}  ({totalBld} зданий, не исследован)", 0, "#4b5563", "#0d1117", false));
                    continue;
                }

                MapPanel.Children.Add(MapRow($"  🏘 {district.Name}  [{expBld}/{totalBld} зд.]", 0, "#a5b4fc", "#0f1535", controlled.Contains(district.Id)));

                foreach (var street in distStreets.Where(s => s.IsExplored).OrderBy(s => s.Name))
                {
                    var buildings = locs.Where(l => l.ParentId == street.Id && l.Type == LocationType.Building).ToList();
                    var commercials = locs.Where(l => l.ParentId == street.Id && l.Type == LocationType.Commercial).ToList();
                    int expB = buildings.Count(b => b.IsExplored);
                    int unkB = buildings.Count - expB;

                    MapPanel.Children.Add(MapRow($"    🛣 {street.Name}  [{expB} иссл.]", 0, "#7dd3fc", "#0a1a28", controlled.Contains(street.Id)));

                    foreach (var bld in buildings.Where(b => b.IsExplored).OrderBy(b => b.Name))
                    {
                        var npcsB = _viewModel.AllNpcs.Where(n => n.IsAlive && n.LocationId == bld.Id).ToList();
                        string status = bld.Status == LocationStatus.Cleared ? "✓" : "⚠";
                        string color = bld.Status == LocationStatus.Cleared ? "#56d364" : "#fbbf24";
                        string npcTag = npcsB.Count > 0 ? $"  👤{npcsB.Count}" : "";
                        MapPanel.Children.Add(MapRow($"      🏢 {bld.Name}  {status}{npcTag}", 0, color, "#111820", controlled.Contains(bld.Id)));

                        foreach (var floor in locs.Where(l => l.ParentId == bld.Id && l.IsExplored).OrderBy(l => l.Name))
                        {
                            var npcsF = _viewModel.AllNpcs.Where(n => n.IsAlive && n.LocationId == floor.Id).ToList();
                            string resLine = floor.ResourceNodes.Count > 0
                                ? "  " + string.Join(" | ", floor.ResourceNodes.Where(kv => kv.Value > 0).Select(kv => $"{kv.Key}:{kv.Value:F0}"))
                                : "";
                            string npcF = npcsF.Count > 0 ? $"  👤{string.Join(",", npcsF.Select(n => n.Name))}" : "";
                            string floorStatus = floor.Status == LocationStatus.Cleared ? "#56d364" : "#f87171";
                            MapPanel.Children.Add(MapRow($"        ▸ {floor.Name}{npcF}{resLine}", 0, floorStatus, "#0d1117", controlled.Contains(floor.Id)));
                        }
                    }

                    // Отображаем коммерческие локации
                    foreach (var comm in commercials.Where(c => c.IsExplored).OrderBy(c => c.Name))
                    {
                        string icon = comm.CommercialType switch
                        {
                            CommercialType.Shop => "🛒",
                            CommercialType.Supermarket => "🏪",
                            CommercialType.Mall => "🏬",
                            CommercialType.Market => "🛍️",
                            CommercialType.Hairdresser => "💈",
                            CommercialType.BeautySalon => "💅",
                            CommercialType.Pharmacy => "💊",
                            CommercialType.Hospital => "🏥",
                            CommercialType.Factory => "🏭",
                            CommercialType.Hotel => "🏨",
                            _ => "🏢"
                        };
                        string status = comm.Status == LocationStatus.Cleared ? "✓" : "⚠";
                        string color = comm.Status == LocationStatus.Cleared ? "#56d364" : "#fbbf24";
                        MapPanel.Children.Add(MapRow($"      {icon} {comm.Name}  {status}", 0, color, "#111820", controlled.Contains(comm.Id)));
                    }

                    if (unkB > 0)
                        MapPanel.Children.Add(MapRow($"      + ещё {unkB} зд. (не исследовано)", 0, "#4b5563", "#0d1117", false));
                }
            }
        }

        if (!_viewModel.Locations.Any())
            MapPanel.Children.Add(new TextBlock { Text = "Карта не загружена.", Foreground = MkBrush("#8b949e"), FontSize = 10 });
    }

    private static Border MapRow(string text, double leftStripWidth, string textColor, string bgColor, bool isProtected)
    {
        string stripColor = isProtected ? "#60a5fa" : textColor;
        var border = new Border
        {
            Background = MkBrush(bgColor),
            BorderBrush = MkBrush(stripColor),
            BorderThickness = new Thickness(2, 0, 0, 0),
            Margin = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(4, 2, 2, 2),
        };
        border.Child = new TextBlock
        {
            Text = text,
            Foreground = MkBrush(textColor),
            FontSize = 10,
            TextWrapping = TextWrapping.NoWrap,
        };
        return border;
    }

    private static System.Windows.Media.SolidColorBrush MkBrush(string hex) =>
        (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString(hex)!;

    private void RefreshTechniquePanel()
    {
        TechniquePanel.Children.Clear();
        foreach (var tech in _viewModel.UnlockedTechniques)
        {
            var btn = new Button
            {
                Content = $"{tech.Name} ({tech.OPCost:F0} ОР)",
                Style = (Style)FindResource("ActionBtn"),
                IsEnabled = _viewModel.DevPoints >= tech.OPCost,
                Opacity = _viewModel.DevPoints >= tech.OPCost ? 1.0 : 0.5,
                ToolTip = tech.Description,
                Tag = tech,
            };
            btn.Click += TechniqueBtn_Click;
            TechniquePanel.Children.Add(btn);
        }
    }

    // =========================================================
    // Actions UI
    // =========================================================

    private void ActionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ActionCombo.SelectedItem is not PlayerActionGroup group) return;

        SubActionRow.Visibility = Visibility.Visible;
        SubActionCombo.Items.Clear();

        foreach (var action in _viewModel.GetActionsByGroup(group.Id))
            SubActionCombo.Items.Add(action);

        SubActionCombo.SelectedIndex = -1;
        ParametersPanel.Children.Clear();
        ParametersPanel.Visibility = Visibility.Collapsed;
    }

    private void SubActionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SubActionCombo.SelectedItem is not PlayerGameAction action)
        {
            _viewModel.SelectedAction = null;
            ParametersPanel.Children.Clear();
            ParametersPanel.Visibility = Visibility.Collapsed;
            return;
        }

        _viewModel.SelectedAction = action;
        BuildParametersUI(action);
        ParametersPanel.Visibility = Visibility.Visible;
    }

    private void BuildParametersUI(PlayerGameAction action)
    {
        ParametersPanel.Children.Clear();
        _dynamicComboBoxes.Clear();

        foreach (var param in action.Parameters.OrderBy(p => p.OrderIndex))
        {
            ParametersPanel.Children.Add(new TextBlock
            {
                Text = param.DisplayName,
                Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#8b949e")!,
                FontSize = 11,
                Margin = new Thickness(0, 8, 0, 4)
            });

            UIElement control = param.ParamType?.Name switch
            {
                "Npc"          => BuildNpcComboBox(param),
                "Resource"     => BuildResourceComboBox(param),
                "Technique"    => BuildTechniqueComboBox(param),
                "Quest"        => BuildQuestComboBox(param),
                "Number"       => BuildNumberBox(param),
                "NumberSlider" => BuildNumberBox(param),
                "Text"         => BuildTextBox(param),
                _ => new TextBlock { Text = $"Тип: {param.ParamType?.Name}", Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#f87171")! }
            };

            ParametersPanel.Children.Add(control);
        }
    }

    private ComboBox BuildNpcComboBox(PlayerActionParam param)
    {
        var combo = new ComboBox { Style = (Style)FindResource("LightCombo"), Tag = param.ParamKey };
        foreach (var npc in _viewModel.AliveNpcs)
            combo.Items.Add(npc.Name);
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        _dynamicComboBoxes.Add(combo);
        return combo;
    }

    private ComboBox BuildResourceComboBox(PlayerActionParam param)
    {
        var combo = new ComboBox { Style = (Style)FindResource("LightCombo"), Tag = param.ParamKey };
        foreach (var res in _viewModel.Resources)
            combo.Items.Add(res.Name);
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        _dynamicComboBoxes.Add(combo);
        return combo;
    }

    private ComboBox BuildTechniqueComboBox(PlayerActionParam param)
    {
        var combo = new ComboBox { Style = (Style)FindResource("LightCombo"), Tag = param.ParamKey };
        foreach (var tech in _viewModel.UnlockedTechniques)
            combo.Items.Add(tech.Name);
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        _dynamicComboBoxes.Add(combo);
        return combo;
    }

    private ComboBox BuildQuestComboBox(PlayerActionParam param)
    {
        var combo = new ComboBox { Style = (Style)FindResource("LightCombo"), Tag = param.ParamKey };
        foreach (var q in _viewModel.AvailableQuests)
            combo.Items.Add(q.Title);
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        _dynamicComboBoxes.Add(combo);
        return combo;
    }

    private TextBox BuildNumberBox(PlayerActionParam param) => new()
    {
        Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#21262d")!,
        Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#c9d1d9")!,
        BorderBrush = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#30363d")!,
        Padding = new Thickness(4, 2, 0, 0),
        Tag = param.ParamKey,
        Text = string.IsNullOrEmpty(param.DefaultValue) ? "1" : param.DefaultValue
    };

    private TextBox BuildTextBox(PlayerActionParam param) => new()
    {
        Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#21262d")!,
        Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#c9d1d9")!,
        BorderBrush = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#30363d")!,
        Padding = new Thickness(4, 2, 0, 0),
        Tag = param.ParamKey,
        Text = param.DefaultValue
    };

    private void ExecuteAction_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedAction == null)
        {
            Log("Выберите действие.", LogEntry.ColorWarning);
            return;
        }

        if (_viewModel.SelectedAction.ConsumesAction && !_viewModel.HasActionsLeft)
        {
            Log($"Час игрока исчерпан ({Player.MaxPlayerActionsPerDay} действий/день).", LogEntry.ColorWarning);
            return;
        }

        var parameters = CollectParameterValues(_viewModel.SelectedAction);

        foreach (var param in _viewModel.SelectedAction.Parameters.Where(p => p.IsRequired))
        {
            if (!parameters.ContainsKey(param.ParamKey) || parameters[param.ParamKey] == null)
            {
                Log($"Не указан параметр: {param.DisplayName}", LogEntry.ColorWarning);
                return;
            }
        }

        var result = _viewModel.ExecuteAction(_viewModel.SelectedAction, parameters);
        if (!string.IsNullOrEmpty(result))
            Log(result, LogEntry.ColorSuccess);

        if (_viewModel.SelectedAction.ConsumesAction)
        {
            _viewModel.ActionsToday++;
            _viewModel.SavePlayer();
        }

        _viewModel.Refresh();
        Refresh();
        UpdateCombosAfterAction();
    }

    private void UpdateCombosAfterAction()
    {
        foreach (var combo in _dynamicComboBoxes)
        {
            var paramKey = combo.Tag?.ToString();
            var param = _viewModel.SelectedAction?.Parameters.FirstOrDefault(p => p.ParamKey == paramKey);
            if (param == null) continue;

            var selected = combo.SelectedItem?.ToString();
            combo.Items.Clear();

            if (param.ParamType?.Name == "Npc")
            {
                foreach (var npc in _viewModel.AliveNpcs)
                    combo.Items.Add(npc.Name);
            }
            else if (param.ParamType?.Name == "Resource")
            {
                foreach (var res in _viewModel.Resources)
                    combo.Items.Add(res.Name);
            }

            if (!string.IsNullOrEmpty(selected) && combo.Items.Contains(selected))
                combo.SelectedItem = selected;
            else if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;
        }
    }

    private Dictionary<string, object> CollectParameterValues(PlayerGameAction action)
    {
        var values = new Dictionary<string, object>();

        foreach (var param in action.Parameters)
        {
            var control = FindControlByTag(ParametersPanel, param.ParamKey);
            if (control == null) continue;

            object? value = param.ParamType?.Name switch
            {
                "Npc"          => GetSelectedNpc(control),
                "Resource"     => GetSelectedResource(control),
                "Technique"    => GetSelectedTechnique(control),
                "Quest"        => GetSelectedQuest(control),
                "Number"       => GetNumberValue(control),
                "NumberSlider" => GetNumberValue(control),
                "Text"         => GetTextValue(control),
                _ => null
            };

            if (value != null)
                values[param.ParamKey] = value;
        }

        return values;
    }

    private FrameworkElement? FindControlByTag(UIElement parent, string tag)
    {
        if (parent is FrameworkElement element && element.Tag?.ToString() == tag)
            return element;

        if (parent is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is UIElement uiChild)
                {
                    var found = FindControlByTag(uiChild, tag);
                    if (found != null) return found;
                }
            }
        }

        if (parent is ContentControl contentControl && contentControl.Content is UIElement contentElement)
        {
            return FindControlByTag(contentElement, tag);
        }

        return null;
    }

    private Npc? GetSelectedNpc(FrameworkElement control)
    {
        if (control is ComboBox combo && combo.SelectedItem != null)
        {
            var name = combo.SelectedItem.ToString();
            return _viewModel.AllNpcs.FirstOrDefault(n => n.Name == name);
        }
        return null;
    }

    private Resource? GetSelectedResource(FrameworkElement control)
    {
        if (control is ComboBox combo && combo.SelectedItem != null)
        {
            var name = combo.SelectedItem.ToString();
            return _viewModel.Resources.FirstOrDefault(r => r.Name == name);
        }
        return null;
    }

    private Technique? GetSelectedTechnique(FrameworkElement control)
    {
        if (control is ComboBox combo && combo.SelectedItem != null)
        {
            var name = combo.SelectedItem.ToString();
            return _viewModel.UnlockedTechniques.FirstOrDefault(t => t.Name == name);
        }
        return null;
    }

    private Quest? GetSelectedQuest(FrameworkElement control)
    {
        if (control is ComboBox combo && combo.SelectedItem != null)
        {
            var title = combo.SelectedItem.ToString();
            return _viewModel.AvailableQuests.FirstOrDefault(q => q.Title == title);
        }
        return null;
    }

    private double GetNumberValue(FrameworkElement control)
    {
        if (control is TextBox box && double.TryParse(box.Text, out double value))
            return value;
        return 1;
    }

    private string GetTextValue(FrameworkElement control)
    {
        return control is TextBox box ? box.Text : string.Empty;
    }

    // =========================================================
    // Tab switching
    // =========================================================

    private void Tab_Click(object sender, RoutedEventArgs e)
    {
        TabActions.IsChecked = sender == TabActions;
        TabMap.IsChecked = sender == TabMap;

        PanelActions.Visibility = TabActions.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PanelMap.Visibility = TabMap.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OpenAltarBtn_Click(object sender, RoutedEventArgs e)
    {
        var win = new AltarWindow(_viewModel);
        win.Owner = Window.GetWindow(this);
        win.ShowDialog();
        _viewModel.Refresh();
        Refresh();
    }

    private void TechniqueBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not Technique tech) return;
        _viewModel.DevPoints -= tech.OPCost;
        _viewModel.SavePlayer();
        var target = tech.HealAmount > 0
            ? _viewModel.AliveNpcs.OrderBy(n => n.Health).FirstOrDefault()
            : _viewModel.AliveNpcs.OrderByDescending(n => n.Initiative).FirstOrDefault();
        if (target == null)
        {
            Log($"«{tech.Name}»: нет живых целей.", LogEntry.ColorWarning);
            Refresh();
            return;
        }
        if (TechniqueSystem.Apply(tech, target, out string techLog))
        {
            _viewModel.SaveNpc(target);
            Log($"  {techLog}", LogEntry.ColorSuccess);
        }
        else
        {
            Log($"«{tech.Name}» не применена: {techLog}", LogEntry.ColorWarning);
        }
        _viewModel.Refresh();
        Refresh();
    }

}