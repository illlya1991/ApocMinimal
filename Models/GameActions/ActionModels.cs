// Models/GameActions/ActionModels.cs
namespace ApocMinimal.Models.GameActions;

public class ActionCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class GameActionDb
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

    public List<ActionConditionDb> Conditions { get; set; } = new();
    public List<ActionEffectDb> Effects { get; set; } = new();
    public List<ActionResourceRequirementDb> ResourceRequirements { get; set; } = new();
}

public class ActionConditionDb
{
    public int Id { get; set; }
    public int ActionId { get; set; }
    public string ConditionType { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public class ActionEffectDb
{
    public int Id { get; set; }
    public int ActionId { get; set; }
    public string EffectType { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public double? Value { get; set; }
    public string? Formula { get; set; }
}

public class ActionResourceRequirementDb
{
    public int Id { get; set; }
    public int ActionId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public double Amount { get; set; }
}