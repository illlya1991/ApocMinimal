using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ApocMinimal.Database;
using ApocMinimal.Models;

namespace ApocMinimal;

internal record TaskDef(string Name, int Days, int RewardResId, double RewardAmt, string Description);

public partial class GameWindow : Window
{
    private readonly DatabaseManager _db;
    private Player         _player    = null!;
    private List<Npc>      _npcs      = new();
    private List<Resource> _resources = new();

    private static readonly TaskDef[] Tasks =
    {
        new("Собрать еду",        3, 1, 5.0,  "→ 5 ед. еды через 3 дня"),
        new("Найти воду",         2, 2, 4.0,  "→ 4 ед. воды через 2 дня"),
        new("Добыть дерево",      3, 4, 6.0,  "→ 6 ед. дерева через 3 дня"),
        new("Найти инструменты",  4, 5, 3.0,  "→ 3 ед. инстр. через 4 дня"),
    };

    // =========================================================
    // Инициализация
    // =========================================================

    public GameWindow(DatabaseManager db)
    {
        InitializeComponent();
        _db = db;
        LoadData();
        BuildActionCombo();
        BuildTaskCombo();
        RefreshAll();
        LogDay($"=== День {_player.CurrentDay} ===");
        Log($"Мир загружен. Выживших: {_npcs.Count(n => n.IsAlive)}", LogEntry.ColorNormal);
    }

    private void LoadData()
    {
        _player    = _db.GetPlayer()!;
        _npcs      = _db.GetAllNpcs();
        _resources = _db.GetAllResources();
    }

    private void BuildActionCombo()
    {
        ActionCombo.Items.Clear();
        ActionCombo.Items.Add("Посмотреть информацию");
        ActionCombo.Items.Add("Передать ресурс");
        ActionCombo.Items.Add("Разговор");
        ActionCombo.Items.Add("Дать задание");
        ActionCombo.SelectedIndex = -1;
    }

    private void BuildTaskCombo()
    {
        TaskCombo.Items.Clear();
        foreach (var t in Tasks)
            TaskCombo.Items.Add($"{t.Name}  ({t.Description})");
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
        RefreshAltarPanel();
    }

    private void RefreshHeader()
    {
        HeaderText.Text =
            $"Игрок: {_player.Name}   |   " +
            $"Алтарь: ур.{_player.AltarLevel}   |   " +
            $"ОВ: {_player.FaithPoints:F0}   |   " +
            $"День: {_player.CurrentDay}";
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
        NpcCardsPanel.Children.Clear();
        foreach (var npc in _npcs)
            NpcCardsPanel.Children.Add(BuildNpcCard(npc));
    }

    private UIElement BuildNpcCard(Npc npc)
    {
        var card = new Border
        {
            Background   = HexBrush(npc.StatusColor),
            CornerRadius = new CornerRadius(4),
            Margin       = new Thickness(6, 4, 6, 0),
            Padding      = new Thickness(8, 6, 8, 6),
            Opacity      = npc.IsAlive ? 1.0 : 0.5,
        };

        var panel = new StackPanel();

        // Имя + черта
        var nameRow = new Grid();
        nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var nameText = new TextBlock
        {
            Text       = npc.IsAlive ? npc.Name : $"✝ {npc.Name}",
            Foreground = Brushes.White,
            FontSize   = 13, FontWeight = FontWeights.SemiBold,
        };
        Grid.SetColumn(nameText, 0);
        var traitText = new TextBlock
        {
            Text      = npc.TraitLabel,
            Foreground = TraitBrush(npc.Trait),
            FontSize   = 10,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(traitText, 1);
        nameRow.Children.Add(nameText);
        nameRow.Children.Add(traitText);
        panel.Children.Add(nameRow);

        if (!npc.IsAlive)
        {
            panel.Children.Add(new TextBlock { Text = "Погиб", Foreground = Brushes.Gray, FontSize = 11 });
            card.Child = panel;
            return card;
        }

        panel.Children.Add(MakeBar("HP",  npc.Health, npc.Health < 30 ? "#f87171" : "#4ade80", "#1a3a1a"));
        panel.Children.Add(MakeBar("🍖", npc.Hunger, npc.Hunger > 70 ? "#f87171" : "#fbbf24",  "#2a2010"));
        panel.Children.Add(MakeBar("💧", npc.Thirst, npc.Thirst > 70 ? "#f87171" : "#60a5fa",  "#10202a"));

        panel.Children.Add(new TextBlock
        {
            Text       = $"Вера: {npc.Faith:F0}  |  {npc.Profession}",
            Foreground = HexBrush("#aaa"),
            FontSize   = 10, Margin = new Thickness(0, 3, 0, 0)
        });

        if (npc.HasTask)
            panel.Children.Add(new TextBlock
            {
                Text       = $"⚙ {npc.ActiveTask} ({npc.TaskDaysLeft} дн.)",
                Foreground = HexBrush("#e879f9"),
                FontSize   = 10
            });

        card.Child = panel;
        return card;
    }

    private static StackPanel MakeBar(string label, double value, string fillHex, string bgHex)
    {
        const double maxWidth = 200;
        var wrap = new StackPanel();
        wrap.Children.Add(new TextBlock
        {
            Text = label, Foreground = Brushes.Gray, FontSize = 9,
            Margin = new Thickness(0, 2, 0, 0)
        });
        var bg = new Border
        {
            Background = HexBrush(bgHex), CornerRadius = new CornerRadius(2), Height = 8,
        };
        var fill = new Border
        {
            Background          = HexBrush(fillHex),
            CornerRadius        = new CornerRadius(2),
            HorizontalAlignment = HorizontalAlignment.Left,
            Width               = Math.Max(0, Math.Min(1, value / 100.0)) * maxWidth,
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

    private void RefreshAltarPanel()
    {
        AltarInfoText.Text =
            $"Уровень: {_player.AltarLevel} / 5\n" +
            $"ОВ: {_player.FaithPoints:F0}\n" +
            $"Лимит в день: {_player.DailyFaithLimit:F0}\n" +
            $"Улучшение: {_player.UpgradeCost} ОВ";

        UpgradeAltarBtn.IsEnabled = _player.CanUpgrade;
        UpgradeAltarBtn.Opacity   = _player.CanUpgrade ? 1.0 : 0.5;
        UpgradeAltarBtn.Content   = _player.AltarLevel >= 5
            ? "Максимальный уровень"
            : $"Улучшить ({_player.UpgradeCost} ОВ)";
    }

    // =========================================================
    // Выбор действия
    // =========================================================

    private void ActionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ActionCombo.SelectedIndex < 0) return;
        var action = ActionCombo.SelectedItem.ToString()!;

        bool showNpc      = true;
        bool showResource = action == "Передать ресурс";
        bool showTask     = action == "Дать задание";

        SetVis(NpcLabel,     showNpc);
        SetVis(NpcCombo,     showNpc);
        SetVis(ResLabel,     showResource);
        SetVis(ResourceCombo,showResource);
        SetVis(AmountRow,    showResource);
        SetVis(TaskLabel,    showTask);
        SetVis(TaskCombo,    showTask);

        ActionButton.Visibility = Visibility.Visible;
        ActionButton.Content = action switch
        {
            "Посмотреть информацию" => "Просмотреть",
            "Передать ресурс"       => "Передать",
            "Разговор"              => "Поговорить",
            "Дать задание"          => "Назначить",
            _                       => "Выполнить"
        };
    }

    // =========================================================
    // Выполнение действий
    // =========================================================

    private void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (NpcCombo.SelectedIndex < 0) { Log("Выберите персонажа.", LogEntry.ColorWarning); return; }

        var npc    = _npcs[NpcCombo.SelectedIndex];
        var action = ActionCombo.SelectedItem?.ToString() ?? "";

        switch (action)
        {
            case "Посмотреть информацию": DoViewInfo(npc);   break;
            case "Передать ресурс":       DoTransfer(npc);   break;
            case "Разговор":              DoChat(npc);       break;
            case "Дать задание":          DoAssignTask(npc); break;
        }
    }

    // --- Посмотреть информацию + 30 статов ---

    private void DoViewInfo(Npc npc)
    {
        Log($"─── {npc.Name} ───────────────────────────────", LogEntry.ColorNormal);
        Log($"  Возраст: {npc.Age} лет   Профессия: {npc.Profession}   Характер: {(npc.Trait == NpcTrait.None ? "обычный" : npc.TraitLabel)}", LogEntry.ColorNormal);
        Log($"  Здоровье: {npc.Health:F0}/100{(npc.Health < 30 ? "  ⚠ КРИТИЧНО" : "")}", HealthColor(npc.Health));
        Log($"  Голод:    {npc.Hunger:F0}/100{(npc.Hunger > 80 ? "  ⚠ ГОЛОДАЕТ" : "")}", NeedColor(npc.Hunger));
        Log($"  Жажда:    {npc.Thirst:F0}/100{(npc.Thirst > 80 ? "  ⚠ ХОЧЕТ ПИТЬ" : "")}", NeedColor(npc.Thirst));
        Log($"  Вера: {npc.Faith:F0}", LogEntry.ColorNormal);
        if (npc.HasTask)
            Log($"  Задание: {npc.ActiveTask} (осталось {npc.TaskDaysLeft} дн.)", LogEntry.ColorSpeech);

        // 30 характеристик — по две в строке
        Log("  --- Характеристики ---", LogEntry.ColorDay);
        var statIds = StatDefs.Names.Keys.OrderBy(k => k).ToList();
        for (int i = 0; i < statIds.Count; i += 2)
        {
            int  id1   = statIds[i];
            var  val1  = npc.Stats.TryGetValue(id1, out var v1) ? v1 : 0;
            string col1 = $"  {StatDefs.Names[id1],-20} {val1,3:F0}";

            if (i + 1 < statIds.Count)
            {
                int  id2  = statIds[i + 1];
                var  val2 = npc.Stats.TryGetValue(id2, out var v2) ? v2 : 0;
                Log($"{col1}    {StatDefs.Names[id2],-20} {val2,3:F0}", StatColor(val1 > val2 ? val1 : val2));
            }
            else
            {
                Log(col1, StatColor(val1));
            }
        }
        Log("───────────────────────────────────────────────", LogEntry.ColorNormal);
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
        if (res.Amount < amount)
        {
            Log($"Недостаточно «{res.Name}». Есть: {res.Amount:F0}.", LogEntry.ColorWarning);
            return;
        }

        res.Amount -= amount;
        _db.SaveResource(res);

        Log($"✓ Передано {amount:F0} ед. «{res.Name}» → {npc.Name}.", LogEntry.ColorSuccess);
        Log($"  Осталось «{res.Name}»: {res.Amount:F0} ед.", LogEntry.ColorNormal);

        RefreshResourceCombo();
        RefreshNpcSidebar();
    }

    // --- Разговор ---

    private void DoChat(Npc npc)
    {
        if (!npc.IsAlive) { Log($"{npc.Name} мёртв.", LogEntry.ColorDanger); return; }

        Log($"Ты обращаешься к {npc.Name}:", LogEntry.ColorNormal);

        string response = (npc.Trait, npc.Faith) switch
        {
            (NpcTrait.Loner, < 50)  => "...(пожимает плечами и отводит взгляд)",
            (NpcTrait.Loner, _)     => "Я справлюсь сам. Спасибо.",
            (NpcTrait.Coward, _) when npc.Health < 40
                                    => "Пожалуйста, не бросай нас! Я боюсь...",
            (NpcTrait.Coward, _)    => "Я... постараюсь. Только это не опасно?",
            (NpcTrait.Leader, > 60) => "Я верую в тебя, Божество! Веду остальных вперёд.",
            (_, >= 70)              => "Я верую в тебя, Божество! Мы выживем вместе.",
            (_, >= 40)              => "Спасибо за заботу. Стараюсь держаться.",
            _                       => "Тяжело. Не знаю, есть ли смысл продолжать...",
        };

        if (npc.Hunger > 80)       response += " Я очень голоден...";
        else if (npc.Thirst > 80)  response += " Мне срочно нужна вода.";

        Log($"  {npc.Name}: «{response}»", LogEntry.ColorSpeech);
    }

    // --- Дать задание ---

    private void DoAssignTask(Npc npc)
    {
        if (!npc.IsAlive)  { Log($"{npc.Name} мёртв.",                              LogEntry.ColorDanger);  return; }
        if (npc.HasTask)   { Log($"{npc.Name} уже выполняет: {npc.ActiveTask}.",     LogEntry.ColorWarning); return; }

        var idx  = TaskCombo.SelectedIndex;
        if (idx < 0) return;
        var task = Tasks[idx];

        if (npc.Trait == NpcTrait.Coward && Random.Shared.Next(2) == 0)
        {
            Log($"{npc.Name} отказался от «{task.Name}». (Трус)", LogEntry.ColorWarning);
            Log($"  {npc.Name}: «Нет-нет-нет, это слишком опасно!»", LogEntry.ColorSpeech);
            return;
        }

        npc.ActiveTask      = task.Name;
        npc.TaskDaysLeft    = task.Days;
        npc.TaskRewardResId = task.RewardResId;
        npc.TaskRewardAmt   = task.RewardAmt;
        _db.SaveNpc(npc);

        Log($"✓ {npc.Name} получил задание: «{task.Name}»", LogEntry.ColorSuccess);
        Log($"  Вернётся через {task.Days} дн. с {task.RewardAmt:F0} ед. ресурса.", LogEntry.ColorNormal);

        RefreshNpcSidebar();
    }

    // =========================================================
    // Завершение дня
    // =========================================================

    private void EndDay_Click(object sender, RoutedEventArgs e)
    {
        _player.CurrentDay++;
        LogDay($"═══ ДЕНЬ {_player.CurrentDay} ════════════════════════");

        ProcessTasks();
        ProcessNeeds();
        ApplyLeaderBonus();
        GenerateFaith();
        AutoConsumeResources();

        _db.SavePlayer(_player);
        foreach (var n in _npcs)      _db.SaveNpc(n);
        foreach (var r in _resources) _db.SaveResource(r);

        Log($"Выживших: {_npcs.Count(n => n.IsAlive)} / {_npcs.Count}  |  ОВ: {_player.FaithPoints:F0}", LogEntry.ColorDay);
        LogDay("═══════════════════════════════════════════");

        RefreshAll();
    }

    private void ProcessTasks()
    {
        foreach (var npc in _npcs.Where(n => n.IsAlive && n.HasTask))
        {
            npc.TaskDaysLeft--;
            if (npc.TaskDaysLeft > 0) continue;

            var res = _resources.FirstOrDefault(r => r.Id == npc.TaskRewardResId);
            if (res != null)
            {
                res.Amount += npc.TaskRewardAmt;
                Log($"✓ {npc.Name} вернулся с «{npc.ActiveTask}» → +{npc.TaskRewardAmt:F0} ед. «{res.Name}».", LogEntry.ColorSuccess);
            }
            npc.ActiveTask = ""; npc.TaskDaysLeft = 0;
            npc.TaskRewardResId = 0; npc.TaskRewardAmt = 0;
        }
    }

    private void ProcessNeeds()
    {
        foreach (var npc in _npcs.Where(n => n.IsAlive))
        {
            npc.Hunger = Math.Min(100, npc.Hunger + 12);
            npc.Thirst = Math.Min(100, npc.Thirst + 16);

            if (npc.Hunger >= 90) { npc.Health = Math.Max(0, npc.Health - 12); Log($"⚠ {npc.Name} голодает! HP → {npc.Health:F0}", LogEntry.ColorDanger); }
            if (npc.Thirst >= 90) { npc.Health = Math.Max(0, npc.Health - 16); Log($"⚠ {npc.Name} обезвожен! HP → {npc.Health:F0}", LogEntry.ColorDanger); }
            if (npc.Health <= 0)   Log($"✝ {npc.Name} погиб.", LogEntry.ColorDanger);
        }
    }

    private void ApplyLeaderBonus()
    {
        foreach (var leader in _npcs.Where(n => n.IsAlive && n.Trait == NpcTrait.Leader))
        {
            var targets = _npcs.Where(n => n.IsAlive && n.Id != leader.Id && n.Trait != NpcTrait.Loner).ToList();
            foreach (var t in targets) t.Faith = Math.Min(100, t.Faith + 3);
            if (targets.Any()) Log($"★ {leader.Name} (Лидер) поднял Веру {targets.Count} выжившим +3.", LogEntry.ColorAltarColor);
        }
    }

    private void GenerateFaith()
    {
        double generated = _npcs.Where(n => n.IsAlive).Sum(n => n.Faith / 100.0 * (1 + _player.AltarLevel * 0.1));
        double gained    = Math.Min(generated, _player.DailyFaithLimit);
        _player.FaithPoints += gained;
        Log($"✦ Получено ОВ: +{gained:F1}  (лимит {_player.DailyFaithLimit:F0})", LogEntry.ColorAltarColor);
    }

    private void AutoConsumeResources()
    {
        int alive = _npcs.Count(n => n.IsAlive);
        if (alive == 0) return;

        var food  = _resources.FirstOrDefault(r => r.Name == "Еда");
        var water = _resources.FirstOrDefault(r => r.Name == "Вода");

        if (food != null)
        {
            double eat = Math.Min(food.Amount, alive);
            food.Amount -= eat;
            int fed = (int)eat;
            foreach (var n in _npcs.Where(n => n.IsAlive).Take(fed)) n.Hunger = Math.Max(0, n.Hunger - 30);
            Log($"🍖 Еда: -{eat:F0} ед.  Осталось: {food.Amount:F0}", LogEntry.ColorNormal);
        }
        if (water != null)
        {
            double drink = Math.Min(water.Amount, alive);
            water.Amount -= drink;
            int watered = (int)drink;
            foreach (var n in _npcs.Where(n => n.IsAlive).Take(watered)) n.Thirst = Math.Max(0, n.Thirst - 35);
            Log($"💧 Вода: -{drink:F0} ед.  Осталось: {water.Amount:F0}", LogEntry.ColorNormal);
        }
    }

    // =========================================================
    // Алтарь
    // =========================================================

    private void UpgradeAltar_Click(object sender, RoutedEventArgs e)
    {
        if (!_player.CanUpgrade) return;
        int cost = _player.UpgradeCost;
        _player.FaithPoints -= cost;
        _player.AltarLevel++;
        _db.SavePlayer(_player);
        Log($"✦ Алтарь улучшен до уровня {_player.AltarLevel}! (потрачено {cost} ОВ)", LogEntry.ColorAltarColor);
        Log($"  Новый лимит ОВ в день: {_player.DailyFaithLimit:F0}", LogEntry.ColorAltarColor);
        RefreshHeader();
        RefreshAltarPanel();
    }

    // =========================================================
    // Лог
    // =========================================================

    private void Log(string text, string color)
    {
        var entry = new LogEntry { Text = $"[{DateTime.Now:HH:mm}]  {text}", Color = color };
        EventLog.Items.Add(entry);
        EventLog.ScrollIntoView(entry);
    }

    private void LogDay(string text)
    {
        var entry = new LogEntry { Text = text, Color = LogEntry.ColorDay };
        EventLog.Items.Add(entry);
        EventLog.ScrollIntoView(entry);
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

    private static string NeedColor(double v) =>
        v > 80 ? LogEntry.ColorDanger : v > 60 ? LogEntry.ColorWarning : LogEntry.ColorNormal;

    private static string StatColor(double v) =>
        v >= 75 ? LogEntry.ColorSuccess : v >= 50 ? LogEntry.ColorNormal : LogEntry.ColorWarning;
}
