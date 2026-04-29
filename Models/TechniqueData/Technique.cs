namespace ApocMinimal.Models.TechniqueData;

public class Technique
{
    public int                     Id              { get; set; }
    public string                  Name            { get; set; } = "";
    public string                  Description     { get; set; } = "";
    public int                     TerminalLevel   { get; set; }
    public TechniqueLevel          TechLevel       { get; set; }
    public TechniqueType           TechType        { get; set; }
    public double                  OPCost          { get; set; }
    public double                  EnergyCost      { get; set; }
    public double                  StaminaCost     { get; set; }
    public Dictionary<int, double> RequiredStats   { get; set; } = new();
    public double                  HealAmount      { get; set; }
    public string                  Faction         { get; set; } = "";
    public string                  CatalogKey      { get; set; } = "";
    public List<ActivationMode>    ActivationModes { get; set; } = new();

    public double TrustBoost   { get; set; }
    public double FearClear    { get; set; }
    public double StaminaBoost { get; set; }

    public string ModesLabel => ActivationModes.Count == 0 ? "—" : $"{ActivationModes.Count} реж.";
}
