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
        // Не вызываем InitializeUI(), так как нет ViewModel
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

        RefreshQuestsTab();
        RefreshAltarTab();
        RefreshMapTab();
        RefreshTechniquePanel();
        RefreshResourceCombo();
    }

    public void Refresh()
    {
        RefreshAltarTab();
        RefreshQuestsTab();
        RefreshMapTab();
        RefreshTechniquePanel();
        RefreshResourceCombo();
        RefreshActionCombo();
    }

    private void RefreshActionCombo()
    {
        if (ActionCombo.SelectedItem is ActionGroup group)
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

    private void RefreshAltarTab()
    {
        AltarInfoLabel.Text =
            $"Уровень: {_viewModel.AltarLevel} / 10\n" +
            $"Вера: {_viewModel.FaithPoints:F0}\n" +
            $"Стоимость улучшения: {_viewModel.UpgradeCost:N0} ОВ";

        UpgradeAltarBtn.IsEnabled = _viewModel.CanUpgrade;
        UpgradeAltarBtn.Content = _viewModel.AltarLevel >= 10
            ? "Максимальный уровень"
            : $"Улучшить ({_viewModel.UpgradeCost:N0} ОВ)";

        BarrierLabel.Text = $"Размер барьера: {_viewModel.BarrierSize:F0} м";

        var sb = new System.Text.StringBuilder("Последователи: ");
        for (int fl = 0; fl <= 5; fl++)
        {
            int lim = _viewModel.GetFollowerLimit(fl);
            if (lim == 0) continue;
            int cur = _viewModel.GetFollowerCountAtLevel(fl);
            sb.Append($"lvl{fl}: {cur}/{(lim == -1 ? "∞" : lim.ToString())}  ");
        }
        FollowerLabel.Text = sb.ToString().TrimEnd();

        AltarTechPanel.Children.Clear();
        foreach (var tech in _viewModel.AllTechniques)
        {
            bool unlocked = tech.AltarLevel <= _viewModel.AltarLevel;
            var btn = new Button
            {
                Content = $"{tech.Name}  ({tech.FaithCost:F0} ОВ)",
                Style = (Style)FindResource("ActionBtn"),
                IsEnabled = unlocked && _viewModel.FaithPoints >= tech.FaithCost,
                Opacity = unlocked ? 1.0 : 0.4,
                ToolTip = tech.Description,
                Tag = tech,
            };
            if (unlocked) btn.Click += TechniqueBtn_Click;
            AltarTechPanel.Children.Add(btn);
        }
    }

    private void RefreshQuestsTab()
    {
        QuestAvailPanel.Children.Clear();
        foreach (var q in _viewModel.AvailableQuests)
            QuestAvailPanel.Children.Add(_uiService.BuildQuestCard(q, false));

        QuestActivePanel.Children.Clear();
        foreach (var q in _viewModel.ActiveQuests)
        {
            var npc = _viewModel.GetNpcById(q.AssignedNpcId);
            QuestActivePanel.Children.Add(_uiService.BuildQuestCard(q, true, npc?.Name));
        }
    }

    private void RefreshMapTab()
    {
        MapPanel.Children.Clear();
        foreach (var city in _viewModel.Locations.Where(l => l.Type == LocationType.City))
            MapPanel.Children.Add(BuildLocationNode(city, 0));
    }

    private UIElement BuildLocationNode(Location loc, int depth)
    {
        var sp = new StackPanel { Margin = new Thickness(depth * 10, 0, 0, 2) };
        var header = new TextBlock
        {
            Text = $"{(loc.IsExplored ? "" : "? ")}{loc.TypeLabel}: {loc.Name}",
            Foreground = loc.IsExplored ?
                (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#c9d1d9")! :
                (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#8b949e")!,
            FontSize = 11,
        };
        if (loc.DangerLevel > 50) header.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#f87171")!;
        sp.Children.Add(header);

        if (loc.IsExplored && loc.ResourceNodes.Count > 0)
            sp.Children.Add(new TextBlock
            {
                Text = "  " + string.Join(", ", loc.ResourceNodes.Select(kv => $"{kv.Key}: {kv.Value:F0}")),
                Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#56d364")!,
                FontSize = 9,
            });

        if (loc.Type > LocationType.Floor)
            foreach (var child in _viewModel.Locations.Where(l => l.ParentId == loc.Id))
                sp.Children.Add(BuildLocationNode(child, depth + 1));

        return sp;
    }

    private void RefreshTechniquePanel()
    {
        TechniquePanel.Children.Clear();
        foreach (var tech in _viewModel.UnlockedTechniques)
        {
            var btn = new Button
            {
                Content = $"{tech.Name} ({tech.FaithCost:F0} ОВ)",
                Style = (Style)FindResource("ActionBtn"),
                IsEnabled = _viewModel.FaithPoints >= tech.FaithCost,
                Opacity = _viewModel.FaithPoints >= tech.FaithCost ? 1.0 : 0.5,
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
        if (ActionCombo.SelectedItem is not ActionGroup group) return;

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
        if (SubActionCombo.SelectedItem is not GameActionDb action)
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

    private void BuildParametersUI(GameActionDb action)
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
                "Npc" => BuildNpcComboBox(param),
                "Resource" => BuildResourceComboBox(param),
                "Number" => BuildNumberBox(param),
                "Text" => BuildTextBox(param),
                _ => new TextBlock { Text = $"Тип: {param.ParamType?.Name}", Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#f87171")! }
            };

            ParametersPanel.Children.Add(control);
        }
    }

    private ComboBox BuildNpcComboBox(ActionParam param)
    {
        var combo = new ComboBox { Style = (Style)FindResource("LightCombo"), Tag = param.ParamKey };
        foreach (var npc in _viewModel.AliveNpcs)
            combo.Items.Add(npc.Name);
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        _dynamicComboBoxes.Add(combo);
        return combo;
    }

    private ComboBox BuildResourceComboBox(ActionParam param)
    {
        var combo = new ComboBox { Style = (Style)FindResource("LightCombo"), Tag = param.ParamKey };
        foreach (var res in _viewModel.Resources)
            combo.Items.Add(res.Name);
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        _dynamicComboBoxes.Add(combo);
        return combo;
    }

    private TextBox BuildNumberBox(ActionParam param) => new()
    {
        Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#21262d")!,
        Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#c9d1d9")!,
        BorderBrush = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#30363d")!,
        Padding = new Thickness(4, 2, 0, 0),
        Tag = param.ParamKey,
        Text = string.IsNullOrEmpty(param.DefaultValue) ? "1" : param.DefaultValue
    };

    private TextBox BuildTextBox(ActionParam param) => new()
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

    private Dictionary<string, object> CollectParameterValues(GameActionDb action)
    {
        var values = new Dictionary<string, object>();

        foreach (var param in action.Parameters)
        {
            var control = FindControlByTag(ParametersPanel, param.ParamKey);
            if (control == null) continue;

            object? value = param.ParamType?.Name switch
            {
                "Npc" => GetSelectedNpc(control),
                "Resource" => GetSelectedResource(control),
                "Number" => GetNumberValue(control),
                "Text" => GetTextValue(control),
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
        TabActions.IsChecked = false; TabQuests.IsChecked = false;
        TabAltar.IsChecked = false; TabMap.IsChecked = false;

        PanelActions.Visibility = Visibility.Collapsed;
        PanelQuests.Visibility = Visibility.Collapsed;
        PanelAltar.Visibility = Visibility.Collapsed;
        PanelMap.Visibility = Visibility.Collapsed;

        if (sender == TabActions) { TabActions.IsChecked = true; PanelActions.Visibility = Visibility.Visible; }
        else if (sender == TabQuests) { TabQuests.IsChecked = true; PanelQuests.Visibility = Visibility.Visible; RefreshQuestsTab(); }
        else if (sender == TabAltar) { TabAltar.IsChecked = true; PanelAltar.Visibility = Visibility.Visible; RefreshAltarTab(); }
        else if (sender == TabMap) { TabMap.IsChecked = true; PanelMap.Visibility = Visibility.Visible; RefreshMapTab(); }
    }

    // =========================================================
    // Button handlers
    // =========================================================

    private void UpgradeAltarBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.CanUpgrade) return;
        long cost = _viewModel.UpgradeCost;
        _viewModel.FaithPoints -= cost;
        _viewModel.AltarLevel++;
        _viewModel.SavePlayer();
        Log($"Алтарь улучшен до уровня {_viewModel.AltarLevel}! (потрачено {cost:N0} ОВ)", LogEntry.ColorAltarColor);
        Log($"  Макс. ОВ/NPC/день: {Player.MaxFaithPerNpcPerDay:F0}  Активных последователей: {_viewModel.MaxActiveFollowers}", LogEntry.ColorAltarColor);
        Refresh();
    }

    private void BarrierBtn_Click(object sender, RoutedEventArgs e)
    {
        const double cost = 20;
        if (_viewModel.FaithPoints < cost) { Log("Недостаточно ОВ для барьера (нужно 20).", LogEntry.ColorWarning); return; }
        _viewModel.FaithPoints -= cost;
        _viewModel.BarrierSize = Math.Min(100, _viewModel.BarrierSize + 10);
        _viewModel.SavePlayer();
        Log($"Барьер укреплён: {_viewModel.BarrierSize:F0} м (-{cost} ОВ)", LogEntry.ColorAltarColor);
        Refresh();
    }

    private void TechniqueBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not Technique tech) return;
        if (_viewModel.FaithPoints < tech.FaithCost) { Log($"Недостаточно ОВ для «{tech.Name}».", LogEntry.ColorWarning); return; }

        _viewModel.FaithPoints -= tech.FaithCost;
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
        Refresh();
    }
}