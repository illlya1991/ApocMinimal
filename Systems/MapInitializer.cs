// Systems/MapInitializer.cs

using ApocMinimal.Models.LocationData;
using ApocMinimal.Models.ResourceData;
using System;
using System.Collections.Generic;
using System.Linq;

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
        ("Еда",          new[]{"магазин","супермаркет","продукт","кафе","ресторан","столов","кухня","склад","квартир","жилой","апарт","рынок"}, 4.0),
        ("Вода",         new[]{"парк","сквер","технич","котельн","подвал","водопровод","озеро","река","колод","колонка"}, 6.0),
        ("Медикаменты",  new[]{"аптек","больниц","поликлин","медиц","клиник","госпитал","ветеринар","квартир","апарт","жилой"}, 2.5),
        ("Дерево",       new[]{"парк","сквер","лес","стройк","склад","гараж","мастерск","квартир","апарт","мебель"}, 5.0),
        ("Инструменты",  new[]{"гараж","мастерск","завод","склад","стройк","автосервис","цех","ремонт","технич"}, 1.5),
        ("Оружие",       new[]{"воен","полиц","охрана","армейск","склад оружия","тир","оружейн"}, 0.5),
        ("Одежда",       new[]{"магазин одежд","секонд","ателье","склад одежд","гардероб","квартир","апарт"}, 2.0),
        ("Электроника",  new[]{"электро","магазин техник","компьютер","телефон","гаджет","склад электроник"}, 1.0),
        ("Топливо",      new[]{"заправк","азс","гараж","нефтебаз","склад гсм","бенз"}, 2.0),
        ("Книги",        new[]{"библиотек","книжн","школ","универ","академ","читальн"}, 2.0),
    };

    // Probability that a location has a resource if name matches
    private const double MatchChance = 0.75;
    // Probability for a non-matching location of the same type (small chance)
    private const double FallbackChance = 0.15;

    /// <summary>
    /// Distributes random resources across all eligible locations.
    /// Marks locations as dirty after modifying their ResourceNodes.
    /// </summary>
    /// <param name="locations">List of all locations</param>
    /// <param name="rnd">Random number generator</param>
    /// <param name="progressCallback">Optional callback for progress reporting (current, total)</param>
    public static void InitialiseMapResources(
        List<Location> locations,
        Random rnd,
        Action<int, int>? progressCallback = null)
    {
        // Only leaf-ish locations (Building, Floor, Apartment, Street, Commercial) get resource nodes
        var eligible = locations.Where(l =>
            l.MapState == MapState.Current &&
            l.Type is LocationType.Building
                     or LocationType.Floor
                     or LocationType.Apartment
                     or LocationType.Street
                     or LocationType.Commercial
        ).ToList();

        if (eligible.Count == 0)
        {
            // Fallback: use all current locations
            eligible = locations.Where(l => l.MapState == MapState.Current).ToList();
        }

        int processed = 0;
        int total = eligible.Count;

        foreach (var loc in eligible)
        {
            loc.ResourceNodes.Clear();
            string nameLower = loc.Name.ToLowerInvariant();
            string fullNameLower = loc.FullName?.ToLowerInvariant() ?? "";

            foreach (var (resource, keywords, baseAmount) in _rules)
            {
                // Check both name and full name for keywords
                bool nameMatch = keywords.Any(k => nameLower.Contains(k) || fullNameLower.Contains(k));
                double chance = nameMatch ? MatchChance : FallbackChance;

                // Special cases based on location type
                if (loc.Type == LocationType.Apartment)
                {
                    // Apartments always have small food/water/medicine/clothing
                    chance = resource switch
                    {
                        "Еда" => 0.9,
                        "Вода" => 0.8,
                        "Медикаменты" => 0.7,
                        "Одежда" => 0.6,
                        "Книги" => 0.4,
                        _ => 0.2
                    };
                }
                else if (loc.Type == LocationType.Commercial)
                {
                    // Commercial locations have higher chances for specific resources
                    chance = loc.CommercialType switch
                    {
                        CommercialType.Pharmacy or CommercialType.Hospital =>
                            resource == "Медикаменты" ? 0.95 : chance * 1.5,
                        CommercialType.Supermarket or CommercialType.Mall or CommercialType.Market =>
                            resource == "Еда" || resource == "Вода" ? 0.9 : chance * 1.3,
                        CommercialType.Factory =>
                            resource == "Инструменты" || resource == "Электроника" ? 0.85 : chance,
                        _ => chance
                    };
                }
                else if (loc.Type == LocationType.Floor)
                {
                    // Floors have reduced chances (resources are more in specific apartments/rooms)
                    chance *= 0.5;
                }

                if (rnd.NextDouble() < chance)
                {
                    // Amount = base ± 50%, rounded to 0.5
                    double amount = baseAmount * (0.5 + rnd.NextDouble());

                    // Apply rarity modifier
                    if (resource == "Оружие" || resource == "Топливо")
                        amount *= 0.5;
                    else if (resource == "Электроника")
                        amount *= 0.7;

                    amount = Math.Round(amount * 2) / 2.0; // round to 0.5
                    loc.ResourceNodes[resource] = Math.Max(0.5, amount);
                }
            }

            // Ensure at least one resource: guaranteed for Apartments, 30% for others
            if (loc.ResourceNodes.Count == 0 &&
                (loc.Type == LocationType.Apartment || rnd.NextDouble() < 0.3))
            {
                var commonResources = new[] { "Еда", "Вода", "Дерево" };
                var randomResource = commonResources[rnd.Next(commonResources.Length)];
                loc.ResourceNodes[randomResource] = Math.Round(rnd.NextDouble() * 3 + 1, 1);
            }

            // Mark location as dirty since we modified its ResourceNodes
            loc.IsDirty = true;

            processed++;

            // Report progress every 100 locations or on the last one
            if (processed % 100 == 0 || processed == total)
            {
                progressCallback?.Invoke(processed, total);
            }
        }

        // Final progress report to ensure 100% is always reported
        progressCallback?.Invoke(total, total);
    }

    /// <summary>
    /// Reduce location node amounts after NPC gathering (simulates depletion).
    /// Automatically marks the location as dirty.
    /// </summary>
    public static void DeductFromNode(Location loc, string resource, double amount)
    {
        if (!loc.ResourceNodes.TryGetValue(resource, out double current))
            return;

        double remaining = current - amount;

        if (remaining <= 0.1) // If less than 0.1, consider it depleted
        {
            loc.ResourceNodes.Remove(resource);
        }
        else
        {
            loc.ResourceNodes[resource] = Math.Round(remaining * 10) / 10.0;
        }

        // Mark as dirty since resource nodes changed
        loc.IsDirty = true;
    }

    /// <summary>
    /// Add resources to a location (e.g., from player actions or events).
    /// Automatically marks the location as dirty.
    /// </summary>
    public static void AddToNode(Location loc, string resource, double amount)
    {
        if (amount <= 0) return;

        if (loc.ResourceNodes.ContainsKey(resource))
        {
            loc.ResourceNodes[resource] += amount;
        }
        else
        {
            loc.ResourceNodes[resource] = amount;
        }

        loc.ResourceNodes[resource] = Math.Round(loc.ResourceNodes[resource] * 10) / 10.0;

        // Mark as dirty
        loc.IsDirty = true;
    }

    /// <summary>
    /// Regenerate resources for a specific location (e.g., after some time passes).
    /// </summary>
    public static void RegenerateResources(Location loc, Random rnd, double regenerationFactor = 0.3)
    {
        string nameLower = loc.Name.ToLowerInvariant();
        string fullNameLower = loc.FullName?.ToLowerInvariant() ?? "";

        foreach (var (resource, keywords, baseAmount) in _rules)
        {
            bool nameMatch = keywords.Any(k => nameLower.Contains(k) || fullNameLower.Contains(k));

            // Only regenerate if location already had this resource or matches keywords
            bool hadResource = loc.ResourceNodes.ContainsKey(resource);
            bool shouldHave = nameMatch || hadResource;

            if (shouldHave && rnd.NextDouble() < 0.2) // 20% chance to regenerate some resources daily
            {
                double regenAmount = baseAmount * regenerationFactor * rnd.NextDouble();
                regenAmount = Math.Round(regenAmount * 10) / 10.0;

                if (regenAmount > 0.1)
                {
                    AddToNode(loc, resource, regenAmount);
                }
            }
        }
    }

    /// <summary>
    /// Get total available resources of a specific type in a location.
    /// </summary>
    public static double GetResourceAmount(Location loc, string resource)
    {
        return loc.ResourceNodes.TryGetValue(resource, out double amount) ? amount : 0;
    }

    /// <summary>
    /// Check if location has any resources available.
    /// </summary>
    public static bool HasAnyResources(Location loc)
    {
        return loc.ResourceNodes.Count > 0 && loc.ResourceNodes.Values.Any(v => v > 0);
    }

    /// <summary>
    /// Get list of all available resources in a location.
    /// </summary>
    public static List<string> GetAvailableResources(Location loc)
    {
        return loc.ResourceNodes.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
    }

    /// <summary>
    /// Get total amount of all resources in a location.
    /// </summary>
    public static double GetTotalResources(Location loc)
    {
        return loc.ResourceNodes.Values.Sum();
    }

    /// <summary>
    /// Get the most abundant resource in a location.
    /// </summary>
    public static string? GetMostAbundantResource(Location loc)
    {
        if (loc.ResourceNodes.Count == 0) return null;
        return loc.ResourceNodes.OrderByDescending(kv => kv.Value).First().Key;
    }
}