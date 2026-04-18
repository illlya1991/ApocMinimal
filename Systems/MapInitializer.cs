using ApocMinimal.Models.LocationData;
using ApocMinimal.Models.ResourceData;

namespace ApocMinimal.Systems;

/// <summary>
/// Distributes resources across map locations on new game start.
/// Uses location name keywords and type to decide what can be found there.
/// Each location gets ResourceNodes — max units findable per action per day.
/// </summary>
public static class MapInitializer
{
    // Resource name → keywords that suggest this resource is present at a location
    private static readonly (string Resource, string[] Keywords, double BasePerAction)[] _rules =
    {
        ("Еда",          new[]{"магазин","супермаркет","продукт","кафе","ресторан","столов","кухня","склад","квартир","жилой","апарт"}, 4.0),
        ("Вода",         new[]{"парк","сквер","технич","котельн","подвал","водопровод","озеро","река","колод"}, 6.0),
        ("Медикаменты",  new[]{"аптек","больниц","поликлин","медиц","квартир","апарт","жилой"}, 2.5),
        ("Дерево",       new[]{"парк","сквер","лес","стройк","склад","гараж","мастерск","квартир","апарт"}, 5.0),
        ("Инструменты",  new[]{"гараж","мастерск","завод","склад","стройк","автосервис","цех"}, 1.5),
    };

    // Probability that a location has a resource if name matches
    private const double MatchChance     = 0.75;
    // Probability for a non-matching location of the same type (small chance)
    private const double FallbackChance  = 0.15;

    public static void InitialiseMapResources(List<Location> locations, Random rnd)
    {
        // Only leaf-ish locations (Building, Floor, Apartment, Street) get resource nodes
        var eligible = locations.Where(l =>
            l.MapState == MapState.Current &&
            l.Type is LocationType.Building or LocationType.Floor or LocationType.Apartment or LocationType.Street
        ).ToList();

        if (eligible.Count == 0)
        {
            // Fallback: use all current locations
            eligible = locations.Where(l => l.MapState == MapState.Current).ToList();
        }

        foreach (var loc in eligible)
        {
            loc.ResourceNodes.Clear();
            string nameLower = loc.Name.ToLowerInvariant();

            foreach (var (resource, keywords, baseAmount) in _rules)
            {
                bool nameMatch = keywords.Any(k => nameLower.Contains(k));
                double chance = nameMatch ? MatchChance : FallbackChance;

                // Apartments always have small food/water/medicine
                if (loc.Type == LocationType.Apartment)
                    chance = resource is "Еда" or "Вода" or "Медикаменты" ? 0.9 : 0.2;

                if (rnd.NextDouble() < chance)
                {
                    // Amount = base ± 50%, rounded to 0.5
                    double amount = baseAmount * (0.5 + rnd.NextDouble());
                    amount = Math.Round(amount * 2) / 2.0; // round to 0.5
                    loc.ResourceNodes[resource] = Math.Max(0.5, amount);
                }
            }
        }
    }

    /// <summary>Reduce location node amounts after NPC gathering (simulates depletion).</summary>
    public static void DeductFromNode(Location loc, string resource, double amount)
    {
        if (!loc.ResourceNodes.TryGetValue(resource, out double current)) return;
        double remaining = current - amount;
        if (remaining <= 0)
            loc.ResourceNodes.Remove(resource);
        else
            loc.ResourceNodes[resource] = Math.Round(remaining * 10) / 10.0;
    }
}
