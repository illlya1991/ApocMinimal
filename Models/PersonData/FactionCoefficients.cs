namespace ApocMinimal.Models.PersonData;

public class FactionCoefficients
{
    public double CoeffDevPerNpc { get; set; } = 1.0;
    public double CoeffDevPerLocation { get; set; } = 0.0;
    public double CoeffDonation { get; set; } = 1.0;
    public double CoeffTerminalUpgradeCost { get; set; } = 1.0;
    public double CoeffStatGrowth { get; set; } = 1.0;
    public double CoeffShopCost { get; set; } = 1.0;
    public double CoeffMaxDevPerNpc { get; set; } = 1.0;
    public double CoeffBarrierUnits { get; set; } = 1.0;

    public static FactionCoefficients ForFaction(PlayerFaction faction, Dictionary<string, double> config)
    {
        string p = $"faction_{faction}_";
        return new FactionCoefficients
        {
            CoeffDevPerNpc            = config.GetValueOrDefault($"{p}dev_per_npc",       1.0),
            CoeffDevPerLocation       = config.GetValueOrDefault($"{p}dev_per_location",  0.0),
            CoeffDonation             = config.GetValueOrDefault($"{p}donation",           1.0),
            CoeffTerminalUpgradeCost  = config.GetValueOrDefault($"{p}terminal_cost",     1.0),
            CoeffStatGrowth           = config.GetValueOrDefault($"{p}stat_growth",       1.0),
            CoeffShopCost             = config.GetValueOrDefault($"{p}shop_cost",         1.0),
            CoeffMaxDevPerNpc         = config.GetValueOrDefault($"{p}max_dev_per_npc",   1.0),
            CoeffBarrierUnits         = config.GetValueOrDefault($"{p}barrier_units",     1.0),
        };
    }
}
