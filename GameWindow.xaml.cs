using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ApocMinimal.Database;
using ApocMinimal.Models;
using ApocMinimal.Systems;

namespace ApocMinimal;

public partial class GameWindow : Window
{
    private readonly DatabaseManager _db;
    private Player         _player    = null!;
    private List<Npc>      _npcs      = new();
    private List<Resource> _resources = new();
    private List<Quest>    _quests    = new();
    private List<Location> _locations = new();

    private readonly Random _rnd = new();

    // =========================================================
    // Инициализация
    // =========================================================

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
        _player    = _db.GetPlayer()!;
        _npcs      = _db.GetAllNpcs();
        _resources = _db.GetAllResources();
        _quests    = _db.GetAllQuests();
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
        DayLabel.Text   = $"  |  День {_player.CurrentDay}";
        FaithLabel.Text = $"  {_player.FaithPoints:F0} веры";
        AltarLabel.Text = $"  Алтарь: ур.{_player.AltarLevel}";
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
            Background   = HexBrush(npc.StatusColor),
            CornerRadius = new CornerRadius(4),
            Margin       = new Thickness(0, 0, 0, 6),
            Padding      = new Thickness(8, 6),
            Opacity      = npc.IsAlive ? 1.0 : 0.5,
        };
        var panel = new StackPanel();

        // Строка: Имя + пол + черта
        var nameRow = new Grid();
        nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var nameText = new TextBlock
        {
            Text       = npc.IsAlive ? $"{npc.Name} ({npc.GenderLabel})" : $"[погиб] {npc.Name}",
            Foreground = Brushes.White, FontSize = 13, FontWeight = FontWeights.SemiBold,
        };
        Grid.SetColumn(nameText, 0);
        var traitText = new TextBlock
        {
            Text = npc.TraitLabel, Foreground = TraitBrush(npc.Trait),
            FontSize = 10, VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(traitText, 1);
        nameRow.Children.Add(nameText);
        nameRow.Children.Add(traitText);
        panel.Children.Add(nameRow);

        if (!npc.IsAlive) { card.Child = panel; return card; }

        // Полосы HP / Выносливость
        panel.Children.Add(MakeBar("HP",       npc.Health,  npc.Health  < 30 ? "#f87171" : "#4ade80", "#1a3a1a"));
        panel.Children.Add(MakeBar("Выносл.",  npc.Stamina, npc.Stamina < 30 ? "#f87171" : "#60a5fa", "#10202a"));

        // Страх / Доверие
        panel.Children.Add(new TextBlock
        {
            Text       = $"Страх: {npc.Fear:F0}  Доверие: {npc.Trust:F0}  Вера: {npc.Faith:F0}",
            Foreground = HexBrush("#8b949e"), FontSize = 10, Margin = new Thickness(0, 2, 0, 0)
        });

        // Профессия + последователь
        panel.Children.Add(new TextBlock
        {
            Text       = $"{npc.Profession}  [{npc.FollowerLabel}]",
            Foreground = HexBrush("#8b949e"), FontSize = 10,
        });

        // Черты характера
        if (npc.CharTraits.Count > 0)
            panel.Children.Add(new TextBlock
            {
                Text       = string.Join(", ", npc.CharTraits.Select(c => c.ToLabel())),
                Foreground = HexBrush("#d29922"), FontSize = 10,
            });

        // Самая срочная потребность
        var urgent = NeedSystem.GetMostUrgentNeed(npc);
        if (urgent != null)
            panel.Children.Add(new TextBlock
            {
                Text       = $"Нужда: {urgent.Name} ({urgent.Value:F0}%)",
                Foreground = urgent.IsCritical ? HexBrush("#f87171") : HexBrush("#fbbf24"),
                FontSize   = 10,
            });

        // Активное задание
        if (npc.HasTask)
            panel.Children.Add(new TextBlock
            {
                Text       = $"Задание: {npc.ActiveTask} ({npc.TaskDaysLeft} дн.)",
                Foreground = HexBrush("#e879f9"), FontSize = 10,
            });

        card.Child = panel;
        return card;
    }

    private static StackPanel MakeBar(string label, double value, string fillHex, string bgHex)
    {
        const double maxWidth = 220;
        var wrap = new StackPanel();
        wrap.Children.Add(new TextBlock
            { Text = label, Foreground = HexBrush("#8b949e"), FontSize = 9, Margin = new Thickness(0, 2, 0, 0) });
        var bg   = new Border { Background = HexBrush(bgHex),   CornerRadius = new CornerRadius(2), Height = 7 };
        var fill = new Border
        {
            Background = HexBrush(fillHex), CornerRadius = new CornerRadius(2),
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
        NpcTrait.Loner  => HexBrush("#94a3b8"),
        _               => Brushes.Transparent,
    };

    private void RefreshAltarTab()
    {
        AltarInfoLabel.Text =
            $"Уровень: {_player.AltarLevel} / 10\n" +
            $"Вера: {_player.FaithPoints:F0}\n" +
            $"Лимит в день: {_player.DailyFaithLimit:F0}\n" +
            $"Стоимость улучшения: {_player.UpgradeCost} ОВ";

        UpgradeAltarBtn.IsEnabled = _player.CanUpgrade;
        UpgradeAltarBtn.Opacity   = _player.CanUpgrade ? 1.0 : 0.5;
        UpgradeAltarBtn.Content   = _player.AltarLevel >= 10
            ? "Максимальный уровень"
            : $"Улучшить ({_player.UpgradeCost} ОВ)";

        BarrierLabel.Text   = $"Размер барьера: {_player.BarrierSize:F0} м";
        FollowerLabel.Text  = $"Последователей: {_npcs.Count(n => n.FollowerLevel > 0)} / {_player.MaxFollowers}";

        AltarTechPanel.Children.Clear();
        foreach (var tech in Player.AllTechniques)
        {
            bool unlocked = tech.AltarLevel <= _player.AltarLevel;
            var btn = new Button
            {
                Content    = $"{tech.Name}  ({tech.FaithCost:F0} ОВ)",
                Style      = (Style)Resources["ActionBtn"],
                IsEnabled  = unlocked && _player.FaithPoints >= tech.FaithCost,
                Opacity    = unlocked ? 1.0 : 0.4,
                ToolTip    = tech.Description,
                Tag        = tech,
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
                Background = HexBrush("#161b22"), BorderBrush = HexBrush("#30363d"),
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 0, 0, 4), Padding = new Thickness(6, 4),
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
            var b   = new Border
            {
                Background = HexBrush("#1a2a1a"), BorderBrush = HexBrush("#30363d"),
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 0, 0, 4), Padding = new Thickness(6, 4),
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
        // Строим дерево: Город → Районы → Здания → Этажи
        var cities = _locations.Where(l => l.Type == LocationType.City).ToList();
        foreach (var city in cities)
            MapPanel.Children.Add(BuildLocationNode(city, 0));
    }

    private UIElement BuildLocationNode(Location loc, int depth)
    {
        var sp = new StackPanel { Margin = new Thickness(depth * 10, 0, 0, 2) };
        var header = new TextBlock
        {
            Text       = $"{(loc.IsExplored ? "" : "? ")}{loc.TypeLabel}: {loc.Name}",
            Foreground = loc.IsExplored ? HexBrush("#c9d1d9") : HexBrush("#8b949e"),
            FontSize   = 11,
        };
        if (loc.DangerLevel > 50)
            header.Foreground = HexBrush("#f87171");
        sp.Children.Add(header);

        if (loc.IsExplored && loc.ResourceNodes.Count > 0)
            sp.Children.Add(new TextBlock
            {
                Text       = "  " + string.Join(", ", loc.ResourceNodes.Select(kv => $"{kv.Key}: {kv.Value:F0}")),
                Foreground = HexBrush("#56d364"), FontSize = 9,
            });

        var children = _locations.Where(l => l.ParentId == loc.Id).ToList();
        // Показываем до уровня Здания (не разворачиваем этажи, чтобы не перегружать)
        if (loc.Type > LocationType.Floor)
            foreach (var child in children)
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
                Content   = $"{tech.Name} ({tech.FaithCost:F0} ОВ)",
                Style     = (Style)Resources["ActionBtn"],
                IsEnabled = canUse,
                Opacity   = canUse ? 1.0 : 0.5,
                ToolTip   = tech.Description,
                Tag       = tech,
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

        bool showNpc      = true;
        bool showResource = action == "Передать ресурс";
        bool showTask     = action == "Дать квест";

        SetVis(NpcLabel,      showNpc);
        SetVis(NpcCombo,      showNpc);
        SetVis(ResLabel,      showResource);
        SetVis(ResourceCombo, showResource);
        SetVis(AmountRow,     showResource);
        SetVis(TaskLabel,     showTask);
        SetVis(TaskCombo,     showTask);
    }

    // =========================================================
    // Выполнение действий
    // =========================================================

    private void ExecuteAction_Click(object sender, RoutedEventArgs e)
    {
        if (NpcCombo.SelectedIndex < 0) { Log("Выберите персонажа.", LogEntry.ColorWarning); return; }

        var npc    = _npcs[NpcCombo.SelectedIndex];
        var action = ActionCombo.SelectedItem?.ToString() ?? "";

        switch (action)
        {
            case "Посмотреть информацию": DoViewInfo(npc);   break;
            case "Передать ресурс":       DoTransfer(npc);   break;
            case "Разговор":              DoChat(npc);       break;
            case "Дать квест":            DoAssignQuest(npc); break;
            default: Log("Выберите действие.", LogEntry.ColorWarning); break;
        }
    }

    // --- Посмотреть информацию ---

    private void DoViewInfo(Npc npc)
    {
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

        // Потребности
        Log("  ПОТРЕБНОСТИ:", LogEntry.ColorDay);
        foreach (var need in npc.Needs.Where(n => n.IsUrgent || n.IsCritical))
            Log($"    {need.Name} [{need.Level}]: {need.Value:F0}% {(need.IsCritical ? "КРИТИЧНО" : "")}", need.IsCritical ? LogEntry.ColorDanger : LogEntry.ColorWarning);

        // 30 характеристик по категориям
        foreach (var (catName, ids) in StatDefs.Categories)
        {
            Log($"  {catName}", LogEntry.ColorDay);
            for (int i = 0; i < ids.Length; i += 2)
            {
                int    id1  = ids[i];
                double val1 = npc.Stats.GetValueOrDefault(id1);
                string line = $"    {StatDefs.Names[id1],-24} {val1,3:F0}";
                if (i + 1 < ids.Length)
                {
                    int    id2  = ids[i + 1];
                    double val2 = npc.Stats.GetValueOrDefault(id2);
                    Log($"{line}    {StatDefs.Names[id2],-24} {val2,3:F0}", StatColor(Math.Max(val1, val2)));
                }
                else
                    Log(line, StatColor(val1));
            }
        }

        // Последние 5 воспоминаний
        if (npc.Memory.Count > 0)
        {
            Log("  ПАМЯТЬ (последние):", LogEntry.ColorDay);
            foreach (var mem in npc.Memory.TakeLast(5))
                Log($"    {mem.Icon} День {mem.Day}: {mem.Text}", LogEntry.ColorNormal);
        }
        Log("──────────────────────────────────────────────", LogEntry.ColorNormal);
    }

    // --- Передать ресурс ---

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
        // Попытаемся удовлетворить соответствующую нужду НПС
        NeedSystem.SatisfyNeed(npc, res.Name, amount * 0.5);
        _db.SaveNpc(npc);

        Log($"Передано {amount:F0} ед. «{res.Name}» → {npc.Name}. Осталось: {res.Amount:F0}", LogEntry.ColorSuccess);
        RefreshResourceCombo();
        RefreshNpcSidebar();
    }

    // --- Разговор ---

    private void DoChat(Npc npc)
    {
        if (!npc.IsAlive) { Log($"{npc.Name} мёртв.", LogEntry.ColorDanger); return; }

        Log($"Ты обращаешься к {npc.Name}:", LogEntry.ColorNormal);
        string response = (npc.Trait, npc.Trust) switch
        {
            (NpcTrait.Loner, < 50)  => "...(пожимает плечами и отводит взгляд)",
            (NpcTrait.Loner, _)     => "Я справлюсь сам. Спасибо.",
            (NpcTrait.Coward, _) when npc.Health < 40 => "Пожалуйста, не бросай нас! Я боюсь...",
            (NpcTrait.Coward, _)    => "Я постараюсь. Только это не опасно?",
            (NpcTrait.Leader, > 60) => "Я верую в тебя, Божество! Веду остальных вперёд.",
            (_, >= 70)              => "Я верую в тебя! Мы выживем вместе.",
            (_, >= 40)              => "Спасибо за заботу. Стараюсь держаться.",
            _                       => "Тяжело. Не знаю, есть ли смысл продолжать...",
        };
        var urgentNeed = NeedSystem.GetMostUrgentNeed(npc);
        if (urgentNeed != null && urgentNeed.IsCritical)
            response += $" Мне срочно нужно: {urgentNeed.Name}!";

        npc.Trust = Math.Min(100, npc.Trust + 2);
        _db.SaveNpc(npc);
        npc.Remember(new MemoryEntry(_player.CurrentDay, MemoryType.Social, "Поговорил с Божеством"));

        Log($"  {npc.Name}: «{response}»", LogEntry.ColorSpeech);
    }

    // --- Дать квест ---

    private void DoAssignQuest(Npc npc)
    {
        if (!npc.IsAlive) { Log($"{npc.Name} мёртв.", LogEntry.ColorDanger); return; }
        if (npc.HasTask)  { Log($"{npc.Name} уже занят: {npc.ActiveTask}.", LogEntry.ColorWarning); return; }

        var availQuests = _quests.Where(q => q.Status == QuestStatus.Available).ToList();
        if (TaskCombo.SelectedIndex < 0 || TaskCombo.SelectedIndex >= availQuests.Count)
        {
            Log("Нет доступных квестов.", LogEntry.ColorWarning); return;
        }
        var quest = availQuests[TaskCombo.SelectedIndex];

        if (npc.Trait == NpcTrait.Coward && _rnd.NextDouble() < 0.5)
        {
            Log($"{npc.Name} (Трус) отказался от «{quest.Title}».", LogEntry.ColorWarning); return;
        }

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
        LogDay($"═══ ДЕНЬ {_player.CurrentDay} ══════════════════════");

        // 1. НПС выполняют свои ежедневные действия
        foreach (var npc in _npcs.Where(n => n.IsAlive))
        {
            var lines = ActionSystem.ProcessDayActions(npc, _rnd, _player.CurrentDay);
            foreach (var l in lines) Log(l, LogEntry.ColorNormal);
        }

        // 2. Продвижение квестов
        var rewards = QuestSystem.AdvanceDay(_quests, _npcs, _rnd);
        foreach (var (npc, q) in rewards)
        {
            var res = _resources.FirstOrDefault(r => r.Id == q.RewardResourceId);
            if (res != null)
            {
                res.Amount += q.RewardAmount;
                Log($"Квест выполнен: «{q.Title}» ({npc.Name}) → +{q.RewardAmount:F0} ед. «{res.Name}»", LogEntry.ColorSuccess);
            }
            else
                Log($"Квест выполнен: «{q.Title}» ({npc.Name})", LogEntry.ColorSuccess);
        }

        // 3. Авто-назначение квестов
        var autoLog = QuestSystem.AutoAssign(_quests, _npcs, _rnd);
        foreach (var l in autoLog) Log(l, LogEntry.ColorNormal);

        // 4. Генерация новых квестов
        var newQuests = QuestSystem.GenerateDailyQuests(_resources, _rnd);
        foreach (var q in newQuests)
        {
            _quests.Add(q);
            _db.InsertQuest(q);
            Log($"Новый квест: «{q.Title}»", LogEntry.ColorDay);
        }

        // 5. Лидерский бонус
        ApplyLeaderBonus();

        // 6. Генерация веры
        GenerateFaith();

        // 7. Авто-потребление ресурсов
        AutoConsumeResources();

        // Сохранение
        _db.SavePlayer(_player);
        foreach (var n in _npcs)      _db.SaveNpc(n);
        foreach (var r in _resources) _db.SaveResource(r);
        foreach (var q in _quests.Where(q => q.Status != QuestStatus.Available)) _db.SaveQuest(q);

        Log($"Выживших: {_npcs.Count(n => n.IsAlive)}/{_npcs.Count}  |  Вера: {_player.FaithPoints:F0}", LogEntry.ColorDay);
        LogDay("══════════════════════════════════════════");

        RefreshAll();
    }

    private void ApplyLeaderBonus()
    {
        foreach (var leader in _npcs.Where(n => n.IsAlive && n.Trait == NpcTrait.Leader))
        {
            var targets = _npcs.Where(n => n.IsAlive && n.Id != leader.Id && n.Trait != NpcTrait.Loner).ToList();
            foreach (var t in targets) t.Faith = Math.Min(100, t.Faith + 3);
            if (targets.Any()) Log($"{leader.Name} (Лидер) поднял Веру {targets.Count} выжившим +3", LogEntry.ColorAltarColor);
        }
    }

    private void GenerateFaith()
    {
        double generated = _npcs.Where(n => n.IsAlive).Sum(n => n.Faith / 100.0 * (1 + _player.AltarLevel * 0.1));
        double gained    = Math.Min(generated, _player.DailyFaithLimit);
        _player.FaithPoints += gained;
        Log($"Получено ОВ: +{gained:F1}  (лимит {_player.DailyFaithLimit:F0})", LogEntry.ColorAltarColor);
    }

    private void AutoConsumeResources()
    {
        int alive = _npcs.Count(n => n.IsAlive);
        if (alive == 0) return;
        var food  = _resources.FirstOrDefault(r => r.Name == "Еда");
        var water = _resources.FirstOrDefault(r => r.Name == "Вода");
        if (food != null)
        {
            double eat = Math.Min(food.Amount, alive * 1.0);
            food.Amount -= eat;
            foreach (var n in _npcs.Where(n => n.IsAlive).Take((int)eat))
                NeedSystem.SatisfyNeed(n, "Еда", 30);
            Log($"Еда: -{eat:F0} ед.  Осталось: {food.Amount:F0}", LogEntry.ColorNormal);
        }
        if (water != null)
        {
            double drink = Math.Min(water.Amount, alive * 1.0);
            water.Amount -= drink;
            foreach (var n in _npcs.Where(n => n.IsAlive).Take((int)drink))
                NeedSystem.SatisfyNeed(n, "Вода", 35);
            Log($"Вода: -{drink:F0} ед.  Осталось: {water.Amount:F0}", LogEntry.ColorNormal);
        }
    }

    // =========================================================
    // Алтарь
    // =========================================================

    private void UpgradeAltarBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!_player.CanUpgrade) return;
        int cost = _player.UpgradeCost;
        _player.FaithPoints -= cost;
        _player.AltarLevel++;
        _db.SavePlayer(_player);
        Log($"Алтарь улучшен до уровня {_player.AltarLevel}! (потрачено {cost} ОВ)", LogEntry.ColorAltarColor);
        Log($"  Новый лимит ОВ в день: {_player.DailyFaithLimit:F0}  Макс. последователей: {_player.MaxFollowers}", LogEntry.ColorAltarColor);
        RefreshAll();
    }

    private void BarrierBtn_Click(object sender, RoutedEventArgs e)
    {
        const double cost = 20;
        if (_player.FaithPoints < cost) { Log("Недостаточно ОВ для барьера (нужно 20).", LogEntry.ColorWarning); return; }
        _player.FaithPoints -= cost;
        _player.BarrierSize  = Math.Min(100, _player.BarrierSize + 10);
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
        Log($"Применена техника «{tech.Name}»: {tech.Description}", LogEntry.ColorAltarColor);

        // Применяем эффект техники
        if (tech.Name == "Благословение" || tech.Name == "Исцеление")
        {
            var target = _npcs.Where(n => n.IsAlive).OrderBy(n => n.Health).FirstOrDefault();
            if (target != null)
            {
                double heal = tech.Name == "Исцеление" ? 100 : 20;
                target.Health = Math.Min(100, target.Health + heal);
                _db.SaveNpc(target);
                Log($"  {target.Name}: здоровье +{heal:F0} → {target.Health:F0}", LogEntry.ColorSuccess);
            }
        }
        RefreshAll();
    }

    // =========================================================
    // Переключение вкладок
    // =========================================================

    private void Tab_Click(object sender, RoutedEventArgs e)
    {
        // Сбрасываем все вкладки
        TabActions.IsChecked = false;
        TabQuests.IsChecked  = false;
        TabAltar.IsChecked   = false;
        TabMap.IsChecked     = false;

        PanelActions.Visibility = Visibility.Collapsed;
        PanelQuests.Visibility  = Visibility.Collapsed;
        PanelAltar.Visibility   = Visibility.Collapsed;
        PanelMap.Visibility     = Visibility.Collapsed;

        if (sender == TabActions) { TabActions.IsChecked = true; PanelActions.Visibility = Visibility.Visible; }
        else if (sender == TabQuests) { TabQuests.IsChecked = true; PanelQuests.Visibility = Visibility.Visible; RefreshQuestsTab(); }
        else if (sender == TabAltar)  { TabAltar.IsChecked  = true; PanelAltar.Visibility  = Visibility.Visible; RefreshAltarTab(); }
        else if (sender == TabMap)    { TabMap.IsChecked    = true; PanelMap.Visibility    = Visibility.Visible; RefreshMapTab(); }
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e) => LogList.Items.Clear();

    // =========================================================
    // Лог
    // =========================================================

    private void Log(string text, string color)
    {
        var entry = new LogEntry { Text = $"[День {_player.CurrentDay}]  {text}", Color = color };
        LogList.Items.Add(entry);
        LogList.ScrollIntoView(entry);
    }

    private void LogDay(string text)
    {
        var entry = new LogEntry { Text = text, Color = LogEntry.ColorDay };
        LogList.Items.Add(entry);
        LogList.ScrollIntoView(entry);
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

    private static string StatColor(double v) =>
        v >= 75 ? LogEntry.ColorSuccess : v >= 50 ? LogEntry.ColorNormal : LogEntry.ColorWarning;
}
