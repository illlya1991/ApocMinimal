using System.Text.Json.Serialization;

namespace ApocMinimal.Models.TechniqueData;

public class TempEffect
{
    public int    StatId   { get; set; }
    public double Value    { get; set; }
    public int    Duration { get; set; } // rounds (combat) or hours (peaceful)
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "ModeType")]
[JsonDerivedType(typeof(PassiveMode),       "Passive")]
[JsonDerivedType(typeof(CombatVsEnemyMode), "CombatVsEnemy")]
[JsonDerivedType(typeof(CombatSelfMode),    "CombatSelf")]
[JsonDerivedType(typeof(PeacefulSelfMode),  "PeacefulSelf")]
[JsonDerivedType(typeof(PeacefulOtherMode), "PeacefulOther")]
[JsonDerivedType(typeof(PeacefulAreaMode),  "PeacefulArea")]
[JsonDerivedType(typeof(PeacefulCraftMode), "PeacefulCraft")]
public abstract class ActivationMode
{
    /// <summary>Stat IDs whose values scale this mode's parameters.</summary>
    public List<int>        StatDeps         { get; set; } = new();
    /// <summary>0–1. Probability of successful activation.</summary>
    public double           ActivationChance { get; set; } = 1.0;
    public List<TempEffect> TempEffects      { get; set; } = new();
}

/// <summary>Always active; can be toggled on/off.</summary>
public class PassiveMode : ActivationMode
{
    public bool IsActive { get; set; } = true;
}

/// <summary>Activated against an enemy in combat.</summary>
public class CombatVsEnemyMode : ActivationMode
{
    public double Accuracy   { get; set; }
    public double Damage     { get; set; }
    public double CritChance { get; set; }
}

/// <summary>Activated on self during combat.</summary>
public class CombatSelfMode : ActivationMode
{
    public double Damage     { get; set; }
    public double ExtraDodge { get; set; }
    public double Defense    { get; set; }
}

/// <summary>Activated on self in peaceful context.</summary>
public class PeacefulSelfMode : ActivationMode
{
    public double Effect     { get; set; }
    public string EffectDesc { get; set; } = "";
}

/// <summary>Activated on another NPC in peaceful context.</summary>
public class PeacefulOtherMode : ActivationMode
{
    public double Effect     { get; set; }
    public string EffectDesc { get; set; } = "";
}

/// <summary>Area-of-effect in peaceful context.</summary>
public class PeacefulAreaMode : ActivationMode
{
    public double Effect     { get; set; }
    public string EffectDesc { get; set; } = "";
}

/// <summary>On item, building or for crafting in peaceful context.</summary>
public class PeacefulCraftMode : ActivationMode
{
    /// <summary>"Item" | "Building" | "Craft"</summary>
    public string CraftTarget { get; set; } = "Craft";
    public double Effect      { get; set; }
    public string EffectDesc  { get; set; } = "";
}
