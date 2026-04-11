namespace ApocMinimal.Models;

public enum GrantedAbilityType { Technique, StatBoost }

public class GrantedAbility
{
    public string AbilityKey { get; set; } = "";
    public GrantedAbilityType Type { get; set; }

    public string TechniqueName { get; set; } = "";
    public TechniqueLevel TechLevel { get; set; }
    public TechniqueType TechType { get; set; }

    public Dictionary<int, double> StatBonuses { get; set; } = new();

    public double OvCost { get; set; }
    public string Description { get; set; } = "";
    public int GrantedOnDay { get; set; }
}
