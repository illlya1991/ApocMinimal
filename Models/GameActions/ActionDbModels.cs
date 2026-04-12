namespace ApocMinimal.Models.GameActions;

/// <summary>
/// Умова дії для бази даних
/// </summary>
public class ActionConditionDb
{
    public int Id { get; set; }
    public int ActionId { get; set; }
    public string ConditionType { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Ефект дії для бази даних
/// </summary>
public class ActionEffectDb
{
    public int Id { get; set; }
    public int ActionId { get; set; }
    public string EffectType { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public double? Value { get; set; }
    public string? Formula { get; set; }
}

/// <summary>
/// Вимога до ресурсів для бази даних
/// </summary>
public class ActionResourceRequirementDb
{
    public int Id { get; set; }
    public int ActionId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public double Amount { get; set; }
}