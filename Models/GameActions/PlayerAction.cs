using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.ResourceData;

namespace ApocMinimal.Models.GameActions;

/// <summary>
/// Категорія дій гравця
/// </summary>
public class PlayerActionCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Дія гравця з бази даних
/// </summary>
public class PlayerActionDb
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string ActionKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequiresTarget { get; set; }
    public bool RequiresResource { get; set; }
    public bool RequiresQuest { get; set; }
    public bool ConsumesAction { get; set; }
    public int ExecutionOrder { get; set; }
    public bool IsActive { get; set; }

    // Навігаційні властивості
    public PlayerActionCategory? Category { get; set; }
    public List<PlayerActionCondition> Conditions { get; set; } = new();
    public List<PlayerActionEffect> Effects { get; set; } = new();
    public List<PlayerActionResourceRequirement> ResourceRequirements { get; set; } = new();
}

/// <summary>
/// Умова виконання дії
/// </summary>
public class PlayerActionCondition
{
    public int Id { get; set; }
    public int ActionId { get; set; }
    public string ConditionType { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Типи умов
/// </summary>
public static class PlayerConditionTypes
{
    public const string NpcAlive = "NpcAlive";
    public const string NpcTrait = "NpcTrait";
    public const string TrustLevel = "TrustLevel";
    public const string HealthLevel = "HealthLevel";
    public const string HasQuest = "HasQuest";
    public const string HasTask = "HasTask";
    public const string PlayerActionLeft = "PlayerActionLeft";
    public const string HasResource = "HasResource";
    public const string FollowerLevel = "FollowerLevel";
}

/// <summary>
/// Оператори порівняння
/// </summary>
public static class PlayerConditionOperators
{
    public const string Equals = "Equals";
    public const string NotEquals = "NotEquals";
    public const string GreaterThan = "GreaterThan";
    public const string GreaterOrEqual = "GreaterOrEqual";
    public const string LessThan = "LessThan";
    public const string LessOrEqual = "LessOrEqual";
    public const string Contains = "Contains";
}

/// <summary>
/// Ефект дії
/// </summary>
public class PlayerActionEffect
{
    public int Id { get; set; }
    public int ActionId { get; set; }
    public string EffectType { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public double? Value { get; set; }
    public string? Formula { get; set; }
}

/// <summary>
/// Типи ефектів
/// </summary>
public static class PlayerEffectTypes
{
    public const string ChangeTrust = "ChangeTrust";
    public const string ChangeHealth = "ChangeHealth";
    public const string ChangeFaith = "ChangeFaith";
    public const string AddMemory = "AddMemory";
    public const string RemoveResource = "RemoveResource";
    public const string AddQuest = "AddQuest";
    public const string ChangeFear = "ChangeFear";
    public const string ChangeStamina = "ChangeStamina";
    public const string ChangeChakra = "ChangeChakra";
    public const string ChangeFollowerLevel = "ChangeFollowerLevel";
}

/// <summary>
/// Цілі ефектів
/// </summary>
public static class PlayerEffectTargets
{
    public const string Self = "Self";
    public const string Npc = "Npc";
    public const string Player = "Player";
    public const string Resource = "Resource";
}

/// <summary>
/// Вимога до ресурсів
/// </summary>
public class PlayerActionResourceRequirement
{
    public int Id { get; set; }
    public int ActionId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public double Amount { get; set; }
}

/// <summary>
/// Залежність між діями
/// </summary>
public class PlayerActionDependency
{
    public int Id { get; set; }
    public int ParentActionId { get; set; }
    public int ChildActionId { get; set; }
    public string DependencyType { get; set; } = string.Empty;
}

/// <summary>
/// Типи залежностей
/// </summary>
public static class PlayerDependencyTypes
{
    public const string Required = "Required";   // Батьківська дія повинна бути виконана
    public const string Disabled = "Disabled";   // Батьківська дія відключає дочірню
    public const string Hidden = "Hidden";       // Батьківська дія ховає дочірню
}