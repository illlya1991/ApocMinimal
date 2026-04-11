using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ApocMinimal.Database;
using ApocMinimal.Models.LocationData;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;
using ApocMinimal.Models.UIData;
using ApocMinimal.Systems;

namespace ApocMinimal;

public partial class GameWindow : Window
{
    private readonly DatabaseManager _db;
    private Player _player = null!;
    private List<Npc> _npcs = new();
    private List<Resource> _resources = new();
    private List<Quest> _quests = new();
    private List<Location> _locations = new();

    private readonly Random _rnd = new();
    private StackPanel? _currentDayPanel;

    public GameWindow(DatabaseManager db)
    {
        InitializeComponent();
        _db = db;
        LoadData();
        BuildActionCombo();
        BuildQuestTaskCombo();
        RefreshAll();
        LogDay($"=== День {_player.CurrentDay} ===");
        Log($"Мир загружен. Выживших: {_npcs.Count(n => n.IsAlive)}", LogEntry.ColorNormal);
    }

    private void LoadData()
    {
        _player = _db.GetPlayer()!;
        _npcs = _db.GetAllNpcs();
        _resources = _db.GetAllResources();
        _quests = _db.GetAllQuests();
        _locations = _db.GetAllLocations();
    }

    private void BuildActionCombo()
    {
        ActionCombo.Items.Clear();
        ActionCombo.Items.Add("Посмотреть информацию");
        ActionCombo.Items.Add("Передать ресурс");
        ActionCombo.Items.Add("Разговор");
        ActionCombo.Items.Add("Дать квест");
        ActionCombo.SelectedIndex = -1;
    }

    private void BuildQuestTaskCombo()
    {
        TaskCombo.Items.Clear();
        foreach (var q in _quests.Where(q => q.Status == QuestStatus.Available))
            TaskCombo.Items.Add(q.Title);
        if (TaskCombo.Items.Count > 0) TaskCombo.SelectedIndex = 0;
    }

    // =========================================================
    // Обновление UI
    // =========================================================

    private void RefreshAll()
    {
        RefreshHeader();
        RefreshNpcCombo();
        RefreshResourceCombo();
        RefreshNpcSidebar();
        RefreshAltarTab();
        RefreshQuestsTab();
        RefreshMapTab();
        RefreshTechniquePanel();
    }

    private void RefreshHeader()
    {
        DayLabel.Text = $"  |  День {_player.CurrentDay}";
        FaithLabel.Text = $"  {_player.FaithPoints:F0} веры";
        AltarLabel.Text = $"  Алтарь: ур.{_player.AltarLevel}";
        bool actionsLeft = _player.PlayerActionsToday < Player.MaxPlayerActionsPerDay;
        ActionsLabel.Text = $"  Действий: {_player.PlayerActionsToday}/{Player.MaxPlayerActionsPerDay}";
        ActionsLabel.Foreground = actionsLeft ? HexBrush("#56d364") : HexBrush("#f87171");
    }

    private void RefreshNpcCombo()
    {
        NpcCombo.Items.Clear();
        foreach (var n in _npcs)
            NpcCombo.Items.Add(n.IsAlive ? n.Name : $"{n.Name} (погиб)");
        if (NpcCombo.Items.Count > 0) NpcCombo.SelectedIndex = 0;
    }

    private void RefreshResourceCombo()
    {
        ResourceCombo.Items.Clear();
        foreach (var r in _resources)
            ResourceCombo.Items.Add($"{r.Name}  ({r.Amount:F0} ед.)");
        if (ResourceCombo.Items.Count > 0) ResourceCombo.SelectedIndex = 0;
    }

    private void RefreshNpcSidebar()
    {
        NpcPanel.Children.Clear();
        foreach (var npc in _npcs)
            NpcPanel.Children.Add(BuildNpcCard(npc));
    }

    private UIElement BuildNpcCard(Npc npc)
    {
        var card = new Border
        {
            Background = HexBrush(npc.StatusColor),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(0, 0, 0, 6),
            Padding = new Thickness(8, 6, 8, 6),
            Opacity = npc.IsAlive ? 1.0 : 0.5,
        };
        var panel = new StackPanel();

        var nameRow = new Grid();
        nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var nameText = new TextBlock
        {
            Text = npc.IsAlive ? $"{npc.Name} ({npc.GenderLabel})" : $"[погиб] {npc.Name}",
            Foreground = Brushes.White,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
        };
        Grid.SetColumn(nameText, 0);
        var traitText = new TextBlock
        {
            Text = npc.TraitLabel,
            Foreground = TraitBrush(npc.Trait),
            FontSize = 10,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(traitText, 1);
        nameRow.Children.Add(nameText);
        nameRow.Children.Add(traitText);
        panel.Children.Add(nameRow);

        if (!npc.IsAlive)
        {
            card.Child = panel;
            card.Cursor = Cursors.Hand;
            card.MouseLeftButtonUp += (_, _) => DoViewInfo(npc);
            return card;
        }

        panel.Children.Add(MakeBar("HP", npc.Health, npc.Health < 30 ? "#f87171" : "#4ade80", "#1a3a1a"));
        panel.Children.Add(MakeBar("Выносл.", npc.Stamina, npc.Stamina < 30 ? "#f87171" : "#60a5fa", "#10202a"));

        panel.Children.Add(new TextBlock
        {
            Text = $"Страх: {npc.Fear:F0}  Доверие: {npc.Trust:F0}  Вера: {npc.Faith:F0}",
            Foreground = HexBrush("#8b949e"),
            FontSize = 10,
            Margin = new Thickness(0, 2, 0, 0)
        });

        // Ключевые характеристики из новой системы
        panel.Children.Add(new TextBlock
        {
            Text = $"Сил:{npc.Stats.Strength.FinalValue}  Лов:{npc.Stats.Agility.FinalValue}  Инт:{npc.Stats.Intelligence.FinalValue}",
            Foreground = HexBrush("#8b949e"),
            FontSize = 10,
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"{npc.Profession}  [{npc.FollowerLabel}]",
            Foreground = HexBrush("#8b949e"),
            FontSize = 10,
        });

        if (npc.CharTraits.Count > 0)
            panel.Children.Add(new TextBlock
            {
                Text = string.Join(", ", npc.CharTraits.Select(c => c.ToLabel())),
                Foreground = HexBrush("#d29922"),
                FontSize = 10,
            });

        var urgent = NeedSystem.GetMostUrgentNeed(npc);
        if (urgent != null)
            panel.Children.Add(new TextBlock
            {
                Text = $"Нужда: {urgent.Name} ({urgent.Value:F0}%)",
                Foreground = urgent.IsCritical ? HexBrush("#f87171") : HexBrush("#fbbf24"),
                FontSize = 10,
            });

        if (npc.HasTask)
            panel.Children.Add(new TextBlock
            {
                Text = $"Задание: {npc.ActiveTask} ({npc.TaskDaysLeft} дн.)",
                Foreground = HexBrush("#e879f9"),
                FontSize = 10,
            });

        card.Child = panel;
        card.Cursor = Cursors.Hand;
        card.MouseLeftButtonUp += (_, _) => DoViewInfo(npc);
        return card;
    }

    private static StackPanel MakeBar(string label, double value, string fillHex, string bgHex)
    {
        const double maxWidth = 220;
        var wrap = new StackPanel();
        wrap.Children.Add(new TextBlock
        { Text = label, Foreground = HexBrush("#8b949e"), FontSize = 9, Margin = new Thickness(0, 2, 0, 0) });
        var bg = new Border { Background = HexBrush(bgHex), CornerRadius = new CornerRadius(2), Height = 7 };
        var fill = new Border
        {
            Background = HexBrush(fillHex),
            CornerRadius = new CornerRadius(2),
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = Math.Max(0, Math.Min(1, value / 100.0)) * maxWidth,
        };
        var grid = new Grid();
        grid.Children.Add(bg);
        grid.Children.Add(fill);
        wrap.Children.Add(grid);
        return wrap;
    }

    private static Brush TraitBrush(NpcTrait t) => t switch
    {
        NpcTrait.Leader => HexBrush("#facc15"),
        NpcTrait.Coward => HexBrush("#f87171"),
        NpcTrait.Loner => HexBrush("#94a3b8"),
        _ => Brushes.Transparent,
    };

    private void RefreshAltarTab()
    {
        // Строка лимитов последователей по уровням
        var limParts = new System.Text.StringBuilder();
        for (int fl = 0; fl <= 5; fl++)
        {
            int lim = _player.GetFollowerLimit(fl);
            if (lim == 0) continue;
            limParts.Append($"  lvl{fl}: {(lim == -1 ? "∞" : lim.ToString())}");
        }

        AltarInfoLabel.Text =
            $"Уровень: {_player.AltarLevel} / 10\n" +
            $"Вера: {_player.FaithPoints:F0}\n" +
            $"Макс. ОВ/NPC/день: {Player.MaxFaithPerNpcPerDay:F0}\n" +
            $"Стоимость улучшения: {_player.UpgradeCost:N0} ОВ";

        UpgradeAltarBtn.IsEnabled = _player.CanUpgrade;
        UpgradeAltarBtn.Opacity = _player.CanUpgrade ? 1.0 : 0.5;
        UpgradeAltarBtn.Content = _player.AltarLevel >= 10
            ? "Максимальный уровень"
            : $"Улучшить ({_player.UpgradeCost:N0} ОВ)";

        BarrierLabel.Text = $"Размер барьера: {_player.BarrierSize:F0} м";

        // Детальные лимиты последователей
        var sb = new System.Text.StringBuilder("Последователи: ");
        for (int fl = 0; fl <= 5; fl++)
        {
            int lim = _player.GetFollowerLimit(fl);
            if (lim == 0) continue;
            int cur = _npcs.Count(n => n.IsAlive && n.FollowerLevel == fl);
            sb.Append($"lvl{fl}: {cur}/{(lim == -1 ? "∞" : lim.ToString())}  ");
        }
        FollowerLabel.Text = sb.ToString().TrimEnd();

        AltarTechPanel.Children.Clear();
        foreach (var tech in Player.AllTechniques)
        {
            bool unlocked = tech.AltarLevel <= _player.AltarLevel;
            var btn = new Button
            {
                Content = $"{tech.Name}  ({tech.FaithCost:F0} ОВ)",
                Style = (Style)Resources["ActionBtn"],
                IsEnabled = unlocked && _player.FaithPoints >= tech.FaithCost,
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
        BuildQuestTaskCombo();

        QuestAvailPanel.Children.Clear();
        foreach (var q in _quests.Where(q => q.Status == QuestStatus.Available))
        {
            var b = new Border
            {
                Background = HexBrush("#161b22"),
                BorderBrush = HexBrush("#30363d"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 0, 0, 4),
                Padding = new Thickness(6, 4, 6, 4),
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = q.Title, Foreground = HexBrush("#c9d1d9"), FontWeight = FontWeights.SemiBold });
            sp.Children.Add(new TextBlock { Text = q.Description, Foreground = HexBrush("#8b949e"), FontSize = 10, TextWrapping = TextWrapping.Wrap });
            sp.Children.Add(new TextBlock { Text = $"Дней: {q.DaysRequired}  Вознаграждение: {q.RewardAmount:F0} ед.", Foreground = HexBrush("#d29922"), FontSize = 10 });
            b.Child = sp;
            QuestAvailPanel.Children.Add(b);
        }
        if (!_quests.Any(q => q.Status == QuestStatus.Available))
            QuestAvailPanel.Children.Add(new TextBlock { Text = "Нет доступных квестов.", Foreground = HexBrush("#8b949e"), FontSize = 11 });

        QuestActivePanel.Children.Clear();
        foreach (var q in _quests.Where(q => q.Status == QuestStatus.Active))
        {
            var npc = _npcs.FirstOrDefault(n => n.Id == q.AssignedNpcId);
            var b = new Border
            {
                Background = HexBrush("#1a2a1a"),
                BorderBrush = HexBrush("#30363d"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 0, 0, 4),
                Padding = new Thickness(6, 4, 6, 4),
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = q.Title, Foreground = HexBrush("#56d364"), FontWeight = FontWeights.SemiBold });
            sp.Children.Add(new TextBlock { Text = $"Исполнитель: {npc?.Name ?? "?"}", Foreground = HexBrush("#8b949e"), FontSize = 10 });
            sp.Children.Add(new TextBlock { Text = $"Осталось дней: {q.DaysRemaining}", Foreground = HexBrush("#d29922"), FontSize = 10 });
            b.Child = sp;
            QuestActivePanel.Children.Add(b);
        }
    }

    private void RefreshMapTab()
    {
        MapPanel.Children.Clear();
        var cities = _locations.Where(l => l.Type == LocationType.City).ToList();
        foreach (var city in cities)
            MapPanel.Children.Add(BuildLocationNode(city, 0));
    }

    private UIElement BuildLocationNode(Location loc, int depth)
    {
        var sp = new StackPanel { Margin = new Thickness(depth * 10, 0, 0, 2) };
        var header = new TextBlock
        {
            Text = $"{(loc.IsExplored ? "" : "? ")}{loc.TypeLabel}: {loc.Name}",
            Foreground = loc.IsExplored ? HexBrush("#c9d1d9") : HexBrush("#8b949e"),
            FontSize = 11,
        };
        if (loc.DangerLevel > 50) header.Foreground = HexBrush("#f87171");
        sp.Children.Add(header);

        if (loc.IsExplored && loc.ResourceNodes.Count > 0)
            sp.Children.Add(new TextBlock
            {
                Text = "  " + string.Join(", ", loc.ResourceNodes.Select(kv => $"{kv.Key}: {kv.Value:F0}")),
                Foreground = HexBrush("#56d364"),
                FontSize = 9,
            });

        if (loc.Type > LocationType.Floor)
            foreach (var child in _locations.Where(l => l.ParentId == loc.Id))
                sp.Children.Add(BuildLocationNode(child, depth + 1));

        return sp;
    }

    private void RefreshTechniquePanel()
    {
        TechniquePanel.Children.Clear();
        foreach (var tech in _player.UnlockedTechniques)
        {
            bool canUse = _player.FaithPoints >= tech.FaithCost;
            var btn = new Button
            {
                Content = $"{tech.Name} ({tech.FaithCost:F0} ОВ)",
                Style = (Style)Resources["ActionBtn"],
                IsEnabled = canUse,
                Opacity = canUse ? 1.0 : 0.5,
                ToolTip = tech.Description,
                Tag = tech,
            };
            btn.Click += TechniqueBtn_Click;
            TechniquePanel.Children.Add(btn);
        }
    }

    // =========================================================
    // Выбор действия
    // =========================================================

    private void ActionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ActionCombo.SelectedIndex < 0) return;
        var action = ActionCombo.SelectedItem?.ToString() ?? "";

        bool showResource = action == "Передать ресурс";
        bool showTask = action == "Дать квест";

        SetVis(NpcLabel, true);
        SetVis(NpcCombo, true);
        SetVis(ResLabel, showResource);
        SetVis(ResourceCombo, showResource);
        SetVis(AmountRow, showResource);
        SetVis(TaskLabel, showTask);
        SetVis(TaskCombo, showTask);
    }

    // =========================================================
    // Выполнение действий
    // =========================================================

    private void ExecuteAction_Click(object sender, RoutedEventArgs e)
    {
        if (NpcCombo.SelectedIndex < 0) { Log("Выберите персонажа.", LogEntry.ColorWarning); return; }

        var action = ActionCombo.SelectedItem?.ToString() ?? "";
        if (string.IsNullOrEmpty(action)) { Log("Выберите действие.", LogEntry.ColorWarning); return; }

        // "Посмотреть информацию" — не тратит действие игрока
        bool consumesAction = action != "Посмотреть информацию";
        if (consumesAction && _player.PlayerActionsToday >= Player.MaxPlayerActionsPerDay)
        {
            Log($"Час игрока исчерпан ({Player.MaxPlayerActionsPerDay} действий/день). Ждите следующего дня.", LogEntry.ColorWarning);
            return;
        }

        var npc = _npcs[NpcCombo.SelectedIndex];

        switch (action)
        {
            case "Посмотреть информацию": DoViewInfo(npc); break;
            case "Передать ресурс": DoTransfer(npc); break;
            case "Разговор": DoChat(npc); break;
            case "Дать квест": DoAssignQuest(npc); break;
            default: Log("Выберите действие.", LogEntry.ColorWarning); return;
        }

        if (consumesAction)
        {
            _player.PlayerActionsToday++;
            _db.SavePlayer(_player);
            RefreshHeader();
        }
    }

    private void DoViewInfo(Npc npc)
    {
        var npcExp = new Expander
        {
            Header = $"[ {npc.Name} — информация ]",
            Style = (Style)Resources["DayExpander"],
            IsExpanded = true,
        };
        var npcContent = new StackPanel { Margin = new Thickness(10, 2, 0, 4) };
        npcExp.Content = npcContent;
        _currentDayPanel?.Children.Add(npcExp);
        var saved = _currentDayPanel;
        _currentDayPanel = npcContent;

        Log($"── {npc.Name} [{npc.GenderLabel}] {npc.Age} лет  {npc.Profession}", LogEntry.ColorDay);
        Log($"  HP:{npc.Health:F0}  Выносл:{npc.Stamina:F0}  Чакра:{npc.Chakra:F0}  Вера:{npc.Faith:F0}", HealthColor(npc.Health));
        Log($"  Страх:{npc.Fear:F0}  Доверие:{npc.Trust:F0}  Инициатива:{npc.Initiative:F0}  Последователь:[{npc.FollowerLabel}]", LogEntry.ColorNormal);
        Log($"  Черты: {string.Join(", ", npc.CharTraits.Select(c => c.ToLabel()))}", LogEntry.ColorNormal);
        if (npc.Emotions.Count > 0)
            Log($"  Эмоции: {string.Join("  ", npc.Emotions.Select(em => $"{em.Name} {em.Percentage:F0}%"))}", LogEntry.ColorSpeech);
        Log($"  Цель: {npc.Goal}", LogEntry.ColorNormal);
        Log($"  Мечта: {npc.Dream}", LogEntry.ColorNormal);
        Log($"  Желание: {npc.Desire}", LogEntry.ColorNormal);
        if (npc.Specializations.Count > 0)
            Log($"  Специализации: {string.Join(", ", npc.Specializations)}", LogEntry.ColorNormal);

        Log("  ПОТРЕБНОСТИ:", LogEntry.ColorDay);
        foreach (var need in npc.Needs.Where(n => n.IsUrgent || n.IsCritical))
            Log($"    {need.Name} [{need.Level}]: {need.Value:F0}% {(need.IsCritical ? "КРИТИЧНО" : "")}",
                need.IsCritical ? LogEntry.ColorDanger : LogEntry.ColorWarning);

        // Новая система характеристик
        Log($"  ФИЗИЧЕСКИЕ ХАРАКТЕРИСТИКИ", LogEntry.ColorDay);
        Log($"    Выносливость:          {npc.Stats.Endurance.FinalValue,3}  (база: {npc.Stats.Endurance.FullBase})", StatColor(npc.Stats.Endurance.FinalValue));
        Log($"    Стойкость:             {npc.Stats.Toughness.FinalValue,3}  (база: {npc.Stats.Toughness.FullBase})", StatColor(npc.Stats.Toughness.FinalValue));
        Log($"    Сила:                  {npc.Stats.Strength.FinalValue,3}  (база: {npc.Stats.Strength.FullBase})", StatColor(npc.Stats.Strength.FinalValue));
        Log($"    Восстановление (физ):  {npc.Stats.RecoveryPhys.FinalValue,3}  (база: {npc.Stats.RecoveryPhys.FullBase})", StatColor(npc.Stats.RecoveryPhys.FinalValue));
        Log($"    Рефлексы:              {npc.Stats.Reflexes.FinalValue,3}  (база: {npc.Stats.Reflexes.FullBase})", StatColor(npc.Stats.Reflexes.FinalValue));
        Log($"    Ловкость:              {npc.Stats.Agility.FinalValue,3}  (база: {npc.Stats.Agility.FullBase})", StatColor(npc.Stats.Agility.FinalValue));
        Log($"    Адаптация:             {npc.Stats.Adaptation.FinalValue,3}  (база: {npc.Stats.Adaptation.FullBase})", StatColor(npc.Stats.Adaptation.FinalValue));
        Log($"    Регенерация:           {npc.Stats.Regeneration.FinalValue,3}  (база: {npc.Stats.Regeneration.FullBase})", StatColor(npc.Stats.Regeneration.FinalValue));
        Log($"    Сенсорика:             {npc.Stats.Sensorics.FinalValue,3}  (база: {npc.Stats.Sensorics.FullBase})", StatColor(npc.Stats.Sensorics.FinalValue));
        Log($"    Долголетие:            {npc.Stats.Longevity.FinalValue,3}  (база: {npc.Stats.Longevity.FullBase})", StatColor(npc.Stats.Longevity.FinalValue));

        Log($"  МЕНТАЛЬНЫЕ ХАРАКТЕРИСТИКИ", LogEntry.ColorDay);
        Log($"    Фокус:                 {npc.Stats.Focus.FinalValue,3}  (база: {npc.Stats.Focus.FullBase})", StatColor(npc.Stats.Focus.FinalValue));
        Log($"    Память:                {npc.Stats.Memory.FinalValue,3}  (база: {npc.Stats.Memory.FullBase})", StatColor(npc.Stats.Memory.FinalValue));
        Log($"    Логика:                {npc.Stats.Logic.FinalValue,3}  (база: {npc.Stats.Logic.FullBase})", StatColor(npc.Stats.Logic.FinalValue));
        Log($"    Дедукция:              {npc.Stats.Deduction.FinalValue,3}  (база: {npc.Stats.Deduction.FullBase})", StatColor(npc.Stats.Deduction.FinalValue));
        Log($"    Интеллект:             {npc.Stats.Intelligence.FinalValue,3}  (база: {npc.Stats.Intelligence.FullBase})", StatColor(npc.Stats.Intelligence.FinalValue));
        Log($"    Воля:                  {npc.Stats.Will.FinalValue,3}  (база: {npc.Stats.Will.FullBase})", StatColor(npc.Stats.Will.FinalValue));
        Log($"    Обучение:              {npc.Stats.Learning.FinalValue,3}  (база: {npc.Stats.Learning.FullBase})", StatColor(npc.Stats.Learning.FinalValue));
        Log($"    Гибкость:              {npc.Stats.Flexibility.FinalValue,3}  (база: {npc.Stats.Flexibility.FullBase})", StatColor(npc.Stats.Flexibility.FinalValue));
        Log($"    Интуиция:              {npc.Stats.Intuition.FinalValue,3}  (база: {npc.Stats.Intuition.FullBase})", StatColor(npc.Stats.Intuition.FinalValue));
        Log($"    Соц. интеллект:        {npc.Stats.SocialIntel.FinalValue,3}  (база: {npc.Stats.SocialIntel.FullBase})", StatColor(npc.Stats.SocialIntel.FinalValue));
        Log($"    Творчество:            {npc.Stats.Creativity.FinalValue,3}  (база: {npc.Stats.Creativity.FullBase})", StatColor(npc.Stats.Creativity.FinalValue));
        Log($"    Математика:            {npc.Stats.Mathematics.FinalValue,3}  (база: {npc.Stats.Mathematics.FullBase})", StatColor(npc.Stats.Mathematics.FinalValue));

        Log($"  ЭНЕРГЕТИЧЕСКИЕ ХАРАКТЕРИСТИКИ", LogEntry.ColorDay);
        Log($"    Запас энергии:         {npc.Stats.EnergyReserve.FinalValue,3}  (база: {npc.Stats.EnergyReserve.FullBase})", StatColor(npc.Stats.EnergyReserve.FinalValue));
        Log($"    Восстановление (энерг):{npc.Stats.EnergyRecovery.FinalValue,3}  (база: {npc.Stats.EnergyRecovery.FullBase})", StatColor(npc.Stats.EnergyRecovery.FinalValue));
        Log($"    Контроль:              {npc.Stats.Control.FinalValue,3}  (база: {npc.Stats.Control.FullBase})", StatColor(npc.Stats.Control.FinalValue));
        Log($"    Концентрация:          {npc.Stats.Concentration.FinalValue,3}  (база: {npc.Stats.Concentration.FullBase})", StatColor(npc.Stats.Concentration.FinalValue));
        Log($"    Выход:                 {npc.Stats.Output.FinalValue,3}  (база: {npc.Stats.Output.FullBase})", StatColor(npc.Stats.Output.FinalValue));
        Log($"    Тонкость:              {npc.Stats.Precision.FinalValue,3}  (база: {npc.Stats.Precision.FullBase})", StatColor(npc.Stats.Precision.FinalValue));
        Log($"    Устойчивость (энерг):  {npc.Stats.EnergyResist.FinalValue,3}  (база: {npc.Stats.EnergyResist.FullBase})", StatColor(npc.Stats.EnergyResist.FinalValue));
        Log($"    Восприятие энергии:    {npc.Stats.EnergySense.FinalValue,3}  (база: {npc.Stats.EnergySense.FullBase})", StatColor(npc.Stats.EnergySense.FinalValue));

        if (npc.Memory.Count > 0)
        {
            Log("  ПАМЯТЬ (последние):", LogEntry.ColorDay);
            foreach (var mem in npc.Memory.TakeLast(5))
                Log($"    {mem.Icon} День {mem.Day}: {mem.Text}", LogEntry.ColorNormal);
        }
        Log("──────────────────────────────────────────────", LogEntry.ColorNormal);

        _currentDayPanel = saved;
        LogScroll.UpdateLayout();
        LogScroll.ScrollToBottom();
    }

    private void DoTransfer(Npc npc)
    {
        if (!npc.IsAlive) { Log($"{npc.Name} мёртв.", LogEntry.ColorDanger); return; }
        if (ResourceCombo.SelectedIndex < 0) return;
        if (!double.TryParse(AmountBox.Text, out var amount) || amount <= 0)
        {
            Log("Введите корректное количество (> 0).", LogEntry.ColorWarning);
            return;
        }
        var res = _resources[ResourceCombo.SelectedIndex];
        if (res.Amount < amount) { Log($"Недостаточно «{res.Name}». Есть: {res.Amount:F0}.", LogEntry.ColorWarning); return; }

        res.Amount -= amount;
        _db.SaveResource(res);
        NeedSystem.SatisfyNeed(npc, res.Name, amount * 0.5);
        _db.SaveNpc(npc);

        Log($"Передано {amount:F0} ед. «{res.Name}» → {npc.Name}. Осталось: {res.Amount:F0}", LogEntry.ColorSuccess);
        RefreshResourceCombo();
        RefreshNpcSidebar();
    }

    private void DoChat(Npc npc)
    {
        if (!npc.IsAlive) { Log($"{npc.Name} мёртв.", LogEntry.ColorDanger); return; }

        Log($"Ты обращаешься к {npc.Name}:", LogEntry.ColorNormal);
        string response = (npc.Trait, npc.Trust) switch
        {
            (NpcTrait.Loner, < 50) => "...(пожимает плечами и отводит взгляд)",
            (NpcTrait.Loner, _) => "Я справлюсь сам. Спасибо.",
            (NpcTrait.Coward, _) when npc.Health < 40 => "Пожалуйста, не бросай нас! Я боюсь...",
            (NpcTrait.Coward, _) => "Я постараюсь. Только это не опасно?",
            (NpcTrait.Leader, > 60) => "Я верую в тебя, Божество! Веду остальных вперёд.",
            (_, >= 70) => "Я верую в тебя! Мы выживем вместе.",
            (_, >= 40) => "Спасибо за заботу. Стараюсь держаться.",
            _ => "Тяжело. Не знаю, есть ли смысл продолжать...",
        };
        var urgentNeed = NeedSystem.GetMostUrgentNeed(npc);
        if (urgentNeed != null && urgentNeed.IsCritical)
            response += $" Мне срочно нужно: {urgentNeed.Name}!";

        npc.Trust = Math.Min(100, npc.Trust + 2);
        _db.SaveNpc(npc);
        npc.Remember(new MemoryEntry(_player.CurrentDay, MemoryType.Social, "Поговорил с Божеством"));

        Log($"  {npc.Name}: «{response}»", LogEntry.ColorSpeech);
    }

    private void DoAssignQuest(Npc npc)
    {
        if (!npc.IsAlive) { Log($"{npc.Name} мёртв.", LogEntry.ColorDanger); return; }
        if (npc.HasTask) { Log($"{npc.Name} уже занят: {npc.ActiveTask}.", LogEntry.ColorWarning); return; }

        var availQuests = _quests.Where(q => q.Status == QuestStatus.Available).ToList();
        if (TaskCombo.SelectedIndex < 0 || TaskCombo.SelectedIndex >= availQuests.Count)
        { Log("Нет доступных квестов.", LogEntry.ColorWarning); return; }

        var quest = availQuests[TaskCombo.SelectedIndex];
        if (npc.Trait == NpcTrait.Coward && _rnd.NextDouble() < 0.5)
        { Log($"{npc.Name} (Трус) отказался от «{quest.Title}».", LogEntry.ColorWarning); return; }

        QuestSystem.AssignQuest(quest, npc);
        _db.SaveNpc(npc);
        _db.SaveQuest(quest);

        Log($"{npc.Name} взял квест: «{quest.Title}» (осталось {quest.DaysRemaining} дн.)", LogEntry.ColorSuccess);
        RefreshAll();
    }

    // =========================================================
    // Завершение дня
    // =========================================================

    private void EndDayBtn_Click(object sender, RoutedEventArgs e)
    {
        _player.CurrentDay++;
        _player.PlayerActionsToday = 0;
        LogDay($"═══ ДЕНЬ {_player.CurrentDay} ══════════════════════");

        var day = GameLoopService.ProcessDay(_player, _npcs, _resources, _quests, _rnd);

        // ── Render NPC action logs ────────────────────────────────────────────
        foreach (var nr in day.NpcResults)
        {
            var npcExp = new Expander
            {
                Header = $"{nr.Npc.Name} — {nr.Actions.Count(x => !x.IsAlert)} действий",
                Style = (Style)Resources["DayExpander"],
                IsExpanded = false,
            };
            var npcPanel = new StackPanel { Margin = new Thickness(10, 2, 0, 2) };
            foreach (var entry in nr.Actions)
                npcPanel.Children.Add(new TextBlock
                {
                    Text = $"[{entry.Time}] {entry.Text}",
                    Foreground = HexBrush(entry.Color),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 1, 0, 0),
                    FontWeight = entry.IsAlert ? FontWeights.Bold : FontWeights.Normal,
                });
            npcExp.Content = npcPanel;
            _currentDayPanel?.Children.Add(npcExp);
        }

        // ── Quest rewards ─────────────────────────────────────────────────────
        foreach (var (npc, q) in day.QuestRewards)
        {
            var res = _resources.FirstOrDefault(r => r.Id == q.RewardResourceId);
            if (res != null)
            {
                res.Amount += q.RewardAmount;
                Log($"Квест выполнен: «{q.Title}» ({npc.Name}) → +{q.RewardAmount:F0} ед. «{res.Name}»", LogEntry.ColorSuccess);
            }
            else Log($"Квест выполнен: «{q.Title}» ({npc.Name})", LogEntry.ColorSuccess);
        }

        // ── New quests ────────────────────────────────────────────────────────
        foreach (var q in day.NewQuests)
        {
            _db.InsertQuest(q);
            Log($"Новый квест: «{q.Title}»", LogEntry.ColorDay);
        }

        // ── General logs (leader bonus, resources, auto-assign) ───────────────
        foreach (var (text, isAlert) in day.Logs)
            Log(text, isAlert ? LogEntry.ColorDanger : LogEntry.ColorNormal);

        // ── Faith summary ─────────────────────────────────────────────────────
        Log($"Получено ОВ: +{day.FaithGained:F1}  (последователей: {day.FollowerCount}, макс. {Player.MaxFaithPerNpcPerDay:F0}/NPC)",
            LogEntry.ColorAltarColor);

        // ── Persist ───────────────────────────────────────────────────────────
        _db.SavePlayer(_player);
        foreach (var n in _npcs) _db.SaveNpc(n);
        foreach (var r in _resources) _db.SaveResource(r);
        foreach (var q in _quests.Where(q => q.Status != QuestStatus.Available)) _db.SaveQuest(q);

        Log($"Выживших: {_npcs.Count(n => n.IsAlive)}/{_npcs.Count}  |  Вера: {_player.FaithPoints:F0}", LogEntry.ColorDay);
        RefreshAll();
    }

    // =========================================================
    // Алтарь
    // =========================================================

    private void UpgradeAltarBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!_player.CanUpgrade) return;
        long cost = _player.UpgradeCost;
        _player.FaithPoints -= cost;
        _player.AltarLevel++;
        _db.SavePlayer(_player);
        Log($"Алтарь улучшен до уровня {_player.AltarLevel}! (потрачено {cost:N0} ОВ)", LogEntry.ColorAltarColor);
        Log($"  Макс. ОВ/NPC/день: {Player.MaxFaithPerNpcPerDay:F0}  Активных последователей: {_player.MaxActiveFollowers}", LogEntry.ColorAltarColor);
        RefreshAll();
    }

    private void BarrierBtn_Click(object sender, RoutedEventArgs e)
    {
        const double cost = 20;
        if (_player.FaithPoints < cost) { Log("Недостаточно ОВ для барьера (нужно 20).", LogEntry.ColorWarning); return; }
        _player.FaithPoints -= cost;
        _player.BarrierSize = Math.Min(100, _player.BarrierSize + 10);
        _db.SavePlayer(_player);
        Log($"Барьер укреплён: {_player.BarrierSize:F0} м (-{cost} ОВ)", LogEntry.ColorAltarColor);
        RefreshAll();
    }

    private void TechniqueBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not Technique tech) return;
        if (_player.FaithPoints < tech.FaithCost) { Log($"Недостаточно ОВ для «{tech.Name}».", LogEntry.ColorWarning); return; }

        _player.FaithPoints -= tech.FaithCost;
        _db.SavePlayer(_player);

        // Pick best target (lowest HP for healing, otherwise random alive NPC)
        var target = tech.HealAmount > 0
            ? _npcs.Where(n => n.IsAlive).OrderBy(n => n.Health).FirstOrDefault()
            : _npcs.Where(n => n.IsAlive).OrderByDescending(n => n.Initiative).FirstOrDefault();

        if (target == null)
        {
            Log($"«{tech.Name}»: нет живых целей.", LogEntry.ColorWarning);
            RefreshAll();
            return;
        }

        if (TechniqueSystem.Apply(tech, target, out string techLog))
        {
            _db.SaveNpc(target);
            Log($"  {techLog}", LogEntry.ColorSuccess);
        }
        else
        {
            Log($"«{tech.Name}» не применена: {techLog}", LogEntry.ColorWarning);
        }
        RefreshAll();
    }

    // =========================================================
    // Переключение вкладок
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

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        LogStack.Children.Clear();
        _currentDayPanel = null;
    }

    private void FilterLog7Days_Click(object sender, RoutedEventArgs e)
    {
        int minDay = _player.CurrentDay - 6; // show last 7 days inclusive
        foreach (var exp in LogStack.Children.OfType<Expander>())
        {
            // Headers like "═══ ДЕНЬ 5 ══..." or "=== День 5 ==="
            string hdr = exp.Header?.ToString() ?? "";
            int day = ParseDayFromHeader(hdr);
            exp.Visibility = (day == 0 || day >= minDay) ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void ShowAllLog_Click(object sender, RoutedEventArgs e)
    {
        foreach (var exp in LogStack.Children.OfType<Expander>())
            exp.Visibility = Visibility.Visible;
    }

    private static int ParseDayFromHeader(string header)
    {
        // Matches "ДЕНЬ 5" or "День 5"
        var match = System.Text.RegularExpressions.Regex.Match(header, @"[Дд]ень\s+(\d+)");
        return match.Success && int.TryParse(match.Groups[1].Value, out int d) ? d : 0;
    }

    private void CollapseAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var child in LogStack.Children.OfType<Expander>())
            SetExpanderRecursive(child, false);
    }

    private void ExpandAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var child in LogStack.Children.OfType<Expander>())
            SetExpanderRecursive(child, true);
    }

    private static void SetExpanderRecursive(Expander exp, bool expanded)
    {
        exp.IsExpanded = expanded;
        if (exp.Content is StackPanel sp)
            foreach (var inner in sp.Children.OfType<Expander>())
                SetExpanderRecursive(inner, expanded);
    }

    // =========================================================
    // Лог
    // =========================================================

    private void Log(string text, string color)
    {
        (_currentDayPanel ?? LogStack.Children.OfType<StackPanel>().LastOrDefault())
            ?.Children.Add(new TextBlock
            {
                Text = text,
                Foreground = HexBrush(color),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 1, 0, 0),
            });
        LogScroll.UpdateLayout();
        LogScroll.ScrollToBottom();
    }

    private void LogDay(string header)
    {
        var expander = new Expander
        {
            Header = header,
            Style = (Style)Resources["DayExpander"],
            IsExpanded = true,
        };
        var content = new StackPanel { Margin = new Thickness(10, 2, 0, 4) };
        expander.Content = content;
        _currentDayPanel = content;
        LogStack.Children.Add(expander);
        LogScroll.UpdateLayout();
        LogScroll.ScrollToBottom();
    }

    // =========================================================
    // Вспомогательные
    // =========================================================

    private static void SetVis(UIElement el, bool visible) =>
        el.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

    private static SolidColorBrush HexBrush(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));

    private static string HealthColor(double hp) =>
        hp < 30 ? LogEntry.ColorDanger : hp < 60 ? LogEntry.ColorWarning : LogEntry.ColorSuccess;

    private static string StatColor(int value) =>
        value >= 75 ? LogEntry.ColorSuccess : value >= 50 ? LogEntry.ColorNormal : LogEntry.ColorWarning;
}