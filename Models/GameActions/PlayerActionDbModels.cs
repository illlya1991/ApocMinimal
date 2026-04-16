// PlayerActionDbModels.cs
namespace ApocMinimal.Models.GameActions
{
    public class PlayerActionConditionDb
    {
        public int Id { get; set; }
        public int ActionId { get; set; }
        public string ConditionType { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class PlayerActionEffectDb
    {
        public int Id { get; set; }
        public int ActionId { get; set; }
        public string EffectType { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public double? Value { get; set; }
        public string? Formula { get; set; }
    }

    public class PlayerActionResourceRequirementDb
    {
        public int Id { get; set; }
        public int ActionId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public double Amount { get; set; }
    }
}