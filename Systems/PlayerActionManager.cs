using ApocMinimal.Database;
using ApocMinimal.Models.GameActions;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;

namespace ApocMinimal.Systems;

/// <summary>
/// Менеджер дій гравця, який працює з базою даних
/// </summary>
public class PlayerActionManager
{
    private readonly DatabaseManager _db;
    private List<PlayerActionDb> _actions = new();
    private Dictionary<string, PlayerActionDb> _actionMap = new();
    private List<PlayerActionCategory> _categories = new();

    public PlayerActionManager(DatabaseManager db)
    {
        _db = db;
        LoadActions();
    }

    /// <summary>
    /// Завантажити всі дії з бази даних
    /// </summary>
    public void LoadActions()
    {
        _categories = _db.GetPlayerActionCategories();
        _actions = _db.GetAllPlayerActionsDb();
        _actionMap = _actions.ToDictionary(a => a.ActionKey, a => a);

        // Завантажуємо умови, ефекти та вимоги для кожної дії
        foreach (var action in _actions)
        {
            action.Conditions = _db.GetPlayerActionConditions(action.Id);
            action.Effects = _db.GetPlayerActionEffects(action.Id);
            action.ResourceRequirements = _db.GetPlayerActionResourceRequirements(action.Id);
            action.Category = _categories.FirstOrDefault(c => c.Id == action.CategoryId);
        }
    }

    /// <summary>
    /// Отримати доступні дії для поточного стану гри
    /// </summary>
    public List<PlayerActionDb> GetAvailableActions(Player player, Npc? target, List<Resource> resources, List<Quest> quests)
    {
        var availableActions = new List<PlayerActionDb>();

        foreach (var action in _actions.Where(a => a.IsActive).OrderBy(a => a.ExecutionOrder))
        {
            if (CheckAllConditions(action, player, target, resources, quests))
                availableActions.Add(action);
        }

        return availableActions;
    }

    /// <summary>
    /// Перевірити всі умови для дії
    /// </summary>
    public bool CheckAllConditions(PlayerActionDb action, Player player, Npc? target, List<Resource> resources, List<Quest> quests)
    {
        foreach (var condition in action.Conditions)
        {
            if (!EvaluateCondition(condition, player, target, resources, quests))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Перевірити одну умову
    /// </summary>
    private bool EvaluateCondition(PlayerActionCondition condition, Player player, Npc? target, List<Resource> resources, List<Quest> quests)
    {
        switch (condition.ConditionType)
        {
            case PlayerConditionTypes.NpcAlive:
                if (target == null) return false;
                return EvaluateBool(target.IsAlive, condition.Operator, condition.Value);

            case PlayerConditionTypes.NpcTrait:
                if (target == null) return false;
                return EvaluateString(target.Trait.ToString(), condition.Operator, condition.Value);

            case PlayerConditionTypes.TrustLevel:
                if (target == null) return false;
                return EvaluateNumeric(target.Trust, condition.Operator, double.Parse(condition.Value));

            case PlayerConditionTypes.HealthLevel:
                if (target == null) return false;
                return EvaluateNumeric(target.Health, condition.Operator, double.Parse(condition.Value));

            case PlayerConditionTypes.HasQuest:
                if (target == null) return false;
                bool hasQuest = quests.Any(q => q.AssignedNpcId == target.Id && q.Status == QuestStatus.Active);
                return EvaluateBool(hasQuest, condition.Operator, condition.Value);

            case PlayerConditionTypes.HasTask:
                if (target == null) return false;
                return EvaluateBool(target.HasTask, condition.Operator, condition.Value);

            case PlayerConditionTypes.PlayerActionLeft:
                int actionsLeft = Player.MaxPlayerActionsPerDay - player.PlayerActionsToday;
                return EvaluateNumeric(actionsLeft, condition.Operator, double.Parse(condition.Value));

            case PlayerConditionTypes.HasResource:
                return CheckResourceCondition(condition, resources);

            case PlayerConditionTypes.FollowerLevel:
                if (target == null) return false;
                return EvaluateNumeric(target.FollowerLevel, condition.Operator, double.Parse(condition.Value));

            default:
                return true;
        }
    }

    private bool EvaluateBool(bool actual, string operator_, string expected)
    {
        bool expectedBool = bool.Parse(expected);
        return operator_ switch
        {
            PlayerConditionOperators.Equals => actual == expectedBool,
            PlayerConditionOperators.NotEquals => actual != expectedBool,
            _ => false
        };
    }

    private bool EvaluateString(string actual, string operator_, string expected)
    {
        return operator_ switch
        {
            PlayerConditionOperators.Equals => actual.Equals(expected, StringComparison.OrdinalIgnoreCase),
            PlayerConditionOperators.Contains => actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
            PlayerConditionOperators.NotEquals => !actual.Equals(expected, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private bool EvaluateNumeric(double actual, string operator_, double expected)
    {
        return operator_ switch
        {
            PlayerConditionOperators.GreaterThan => actual > expected,
            PlayerConditionOperators.GreaterOrEqual => actual >= expected,
            PlayerConditionOperators.LessThan => actual < expected,
            PlayerConditionOperators.LessOrEqual => actual <= expected,
            PlayerConditionOperators.Equals => Math.Abs(actual - expected) < 0.01,
            PlayerConditionOperators.NotEquals => Math.Abs(actual - expected) > 0.01,
            _ => false
        };
    }

    private bool CheckResourceCondition(PlayerActionCondition condition, List<Resource> resources)
    {
        // Формат Value: "ResourceName:Amount" або просто "ResourceName"
        var parts = condition.Value.Split(':');
        string resourceName = parts[0];
        double requiredAmount = parts.Length > 1 ? double.Parse(parts[1]) : 1;

        var resource = resources.FirstOrDefault(r => r.Name.Equals(resourceName, StringComparison.OrdinalIgnoreCase));
        if (resource == null) return false;

        return condition.Operator switch
        {
            PlayerConditionOperators.GreaterThan => resource.Amount > requiredAmount,
            PlayerConditionOperators.GreaterOrEqual => resource.Amount >= requiredAmount,
            PlayerConditionOperators.LessThan => resource.Amount < requiredAmount,
            PlayerConditionOperators.LessOrEqual => resource.Amount <= requiredAmount,
            PlayerConditionOperators.Equals => Math.Abs(resource.Amount - requiredAmount) < 0.01,
            _ => resource.Amount >= requiredAmount
        };
    }

    /// <summary>
    /// Перевірити наявність ресурсів для дії
    /// </summary>
    public bool CheckResourceRequirements(PlayerActionDb action, List<Resource> resources)
    {
        foreach (var req in action.ResourceRequirements)
        {
            var resource = resources.FirstOrDefault(r => r.Name.Equals(req.ResourceName, StringComparison.OrdinalIgnoreCase));
            if (resource == null || resource.Amount < req.Amount)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Виконати дію та застосувати всі ефекти
    /// </summary>
    public string ExecuteAction(PlayerActionDb action, Player player, Npc target, List<Resource> resources, List<Quest> quests, Random rnd)
    {
        var results = new List<string>();

        // Перевіряємо ресурси
        if (!CheckResourceRequirements(action, resources))
            return "Недостатньо ресурсів!";

        // Витрачаємо ресурси
        foreach (var req in action.ResourceRequirements)
        {
            var resource = resources.First(r => r.Name.Equals(req.ResourceName, StringComparison.OrdinalIgnoreCase));
            resource.Amount -= req.Amount;
            results.Add($"Витрачено {req.Amount} {req.ResourceName}");
            _db.SaveResource(resource);
        }

        // Застосовуємо ефекти
        foreach (var effect in action.Effects)
        {
            var result = ApplyEffect(effect, player, target, resources, quests, rnd);
            if (!string.IsNullOrEmpty(result))
                results.Add(result);
        }

        _db.SavePlayer(player);
        _db.SaveNpc(target);

        return string.Join("; ", results);
    }

    /// <summary>
    /// Застосувати один ефект
    /// </summary>
    private string ApplyEffect(PlayerActionEffect effect, Player player, Npc target, List<Resource> resources, List<Quest> quests, Random rnd)
    {
        double value = effect.Value ?? 0;

        // Обчислюємо значення за формулою якщо є
        if (!string.IsNullOrEmpty(effect.Formula))
        {
            value = EvaluateFormula(effect.Formula, target);
        }

        switch (effect.EffectType)
        {
            case PlayerEffectTypes.ChangeTrust:
                target.Trust = Math.Min(100, Math.Max(0, target.Trust + value));
                return $"Довіра {target.Name}: {(value > 0 ? "+" : "")}{value:F0}";

            case PlayerEffectTypes.ChangeHealth:
                target.Health = Math.Min(100, Math.Max(0, target.Health + value));
                return $"Здоров'я {target.Name}: {(value > 0 ? "+" : "")}{value:F0}";

            case PlayerEffectTypes.ChangeFaith:
                player.FaithPoints += value;
                return $"Отримано {value:F0} віри";

            case PlayerEffectTypes.AddMemory:
                target.Remember(new MemoryEntry(player.CurrentDay, MemoryType.Social, effect.Formula ?? "Взаємодія з Божеством"));
                return $"Додано спогад для {target.Name}";

            case PlayerEffectTypes.RemoveResource:
                var resource = resources.FirstOrDefault(r => r.Name.Equals(effect.Target, StringComparison.OrdinalIgnoreCase));
                if (resource != null)
                {
                    resource.Amount -= value;
                    return $"Вилучено {value} {resource.Name}";
                }
                break;

            case PlayerEffectTypes.ChangeFear:
                target.Fear = Math.Min(100, Math.Max(0, target.Fear + value));
                return $"Страх {target.Name}: {(value > 0 ? "+" : "")}{value:F0}";

            case PlayerEffectTypes.ChangeStamina:
                target.Stamina = Math.Min(100, Math.Max(0, target.Stamina + value));
                return $"Витривалість {target.Name}: {(value > 0 ? "+" : "")}{value:F0}";

            case PlayerEffectTypes.ChangeChakra:
                target.Chakra = Math.Min(100, Math.Max(0, target.Chakra + value));
                return $"Чакра {target.Name}: {(value > 0 ? "+" : "")}{value:F0}";

            case PlayerEffectTypes.ChangeFollowerLevel:
                target.FollowerLevel = Math.Min(5, Math.Max(0, target.FollowerLevel + (int)value));
                return $"Рівень послідовника {target.Name}: {(value > 0 ? "+" : "")}{value:F0}";
        }

        return string.Empty;
    }

    /// <summary>
    /// Обчислити формулу (підтримує змінні: trust, health, fear, stamina, chakra, faith)
    /// </summary>
    private double EvaluateFormula(string formula, Npc target)
    {
        var result = formula
            .Replace("trust", target.Trust.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Replace("health", target.Health.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Replace("fear", target.Fear.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Replace("stamina", target.Stamina.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Replace("chakra", target.Chakra.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Replace("faith", target.Faith.ToString(System.Globalization.CultureInfo.InvariantCulture));

        // Спроба обчислити простий математичний вираз
        try
        {
            using System.Data.DataTable dt = new();
            var computed = dt.Compute(result, "");
            return Convert.ToDouble(computed);
        }
        catch
        {
            // Якщо не вийшло, пробуємо просто парсити
            if (double.TryParse(result, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double value))
                return value;
            return 0;
        }
    }

    /// <summary>
    /// Отримати дію за ключем
    /// </summary>
    public PlayerActionDb? GetActionByKey(string key)
    {
        return _actionMap.GetValueOrDefault(key);
    }

    /// <summary>
    /// Отримати всі дії
    /// </summary>
    public List<PlayerActionDb> GetAllActions() => _actions;

    /// <summary>
    /// Отримати категорії
    /// </summary>
    public List<PlayerActionCategory> GetAllCategories() => _categories;
}