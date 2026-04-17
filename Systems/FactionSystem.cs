using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.PlayerData;

namespace ApocMinimal.Systems;

public enum FactionRelation { Ally = 2, Friendly = 1, Neutral = 0, Hostile = -1, Enemy = -2 }

public class Faction
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Leader { get; set; } = "";
    public int MemberCount { get; set; }
    public double TerritoryStrength { get; set; } // 0–100
    public FactionRelation PlayerRelation { get; set; } = FactionRelation.Neutral;
    public Dictionary<int, FactionRelation> FactionRelations { get; set; } = new();

    public string RelationLabel => PlayerRelation switch
    {
        FactionRelation.Ally    => "Союзник",
        FactionRelation.Friendly=> "Дружественный",
        FactionRelation.Neutral => "Нейтральный",
        FactionRelation.Hostile => "Враждебный",
        FactionRelation.Enemy   => "Враг",
        _                       => "Неизвестно",
    };

    public string RelationColor => PlayerRelation switch
    {
        FactionRelation.Ally     => "#22c55e",
        FactionRelation.Friendly => "#86efac",
        FactionRelation.Neutral  => "#94a3b8",
        FactionRelation.Hostile  => "#f97316",
        FactionRelation.Enemy    => "#ef4444",
        _                        => "#94a3b8",
    };
}

public class FactionEvent
{
    public int Day { get; set; }
    public string Description { get; set; } = "";
    public FactionRelation RelationChange { get; set; }
}

/// <summary>
/// Manages factions: creation, daily relation drift, conflicts, alliances, player diplomacy.
/// </summary>
public static class FactionSystem
{
    // ── Pre-defined factions ──────────────────────────────────────────────────

    public static List<Faction> CreateDefaultFactions() => new()
    {
        new Faction
        {
            Id = 1, Name = "Совет выживших",
            Description = "Организованная группа, управляющая центральным кварталом. Придерживаются законов старого мира.",
            Leader = "Капитан Зорин", MemberCount = 45, TerritoryStrength = 60,
            PlayerRelation = FactionRelation.Neutral,
        },
        new Faction
        {
            Id = 2, Name = "Банда Ржавого",
            Description = "Жестокие мародёры, контролирующие промышленный район. Нападают на слабых.",
            Leader = "Ржавый", MemberCount = 25, TerritoryStrength = 40,
            PlayerRelation = FactionRelation.Hostile,
        },
        new Faction
        {
            Id = 3, Name = "Лесные братья",
            Description = "Самодостаточная колония в парке. Живут охотой и огородничеством.",
            Leader = "Старейшина Клара", MemberCount = 18, TerritoryStrength = 30,
            PlayerRelation = FactionRelation.Friendly,
        },
        new Faction
        {
            Id = 4, Name = "Технократы",
            Description = "Учёные и инженеры в бывшем университете. Разрабатывают технологии выживания.",
            Leader = "Профессор Нестеров", MemberCount = 12, TerritoryStrength = 20,
            PlayerRelation = FactionRelation.Neutral,
        },
        new Faction
        {
            Id = 5, Name = "Армия Президента",
            Description = "Остатки государственных силовых структур. Ищут баланс власти.",
            Leader = "Генерал Петров", MemberCount = 80, TerritoryStrength = 90,
            PlayerRelation = FactionRelation.Neutral,
        },
    };

    // ── Daily simulation ──────────────────────────────────────────────────────

    /// <summary>Simulate one day of inter-faction dynamics.</summary>
    public static List<FactionEvent> SimulateDay(List<Faction> factions, int day, Random rnd)
    {
        var events = new List<FactionEvent>();

        // Natural drift: hostile factions grow slightly more hostile
        for (int i = 0; i < factions.Count; i++)
        {
            if (factions[i].PlayerRelation == FactionRelation.Hostile && rnd.NextDouble() < 0.05)
            {
                factions[i].TerritoryStrength = Math.Min(100, factions[i].TerritoryStrength + 1);
            }
        }

        // Random inter-faction conflict (rare)
        if (rnd.NextDouble() < 0.08 && factions.Count >= 2)
        {
            var f1 = factions[rnd.Next(factions.Count)];
            Faction f2;
            do { f2 = factions[rnd.Next(factions.Count)]; } while (f2.Id == f1.Id);

            var rel = f1.FactionRelations.TryGetValue(f2.Id, out var r) ? r : FactionRelation.Neutral;
            if (rel <= FactionRelation.Hostile)
            {
                int loss = rnd.Next(2, 8);
                f1.MemberCount = Math.Max(1, f1.MemberCount - loss / 2);
                f2.MemberCount = Math.Max(1, f2.MemberCount - loss / 2);
                events.Add(new FactionEvent
                {
                    Day = day,
                    Description = $"Столкновение: «{f1.Name}» vs «{f2.Name}» — обе стороны потеряли по {loss/2} чел.",
                });
            }
        }

        return events;
    }

    // ── Player diplomacy ──────────────────────────────────────────────────────

    /// <summary>Player donates resources → faction becomes more friendly.</summary>
    public static string Donate(Faction faction, double amount)
    {
        int shift = amount >= 50 ? 2 : amount >= 20 ? 1 : 0;
        if (shift == 0) return "Слишком мало для смены отношений.";

        faction.PlayerRelation = Clamp((int)faction.PlayerRelation + shift);
        return $"«{faction.Name}»: отношения улучшились → {faction.RelationLabel}";
    }

    /// <summary>Player attacks a faction location → relation worsens.</summary>
    public static string Attack(Faction faction, int damage)
    {
        faction.TerritoryStrength = Math.Max(0, faction.TerritoryStrength - damage);
        faction.PlayerRelation = Clamp((int)faction.PlayerRelation - 1);
        return $"«{faction.Name}»: атака нанесла {damage} ед. урона. Отношения: {faction.RelationLabel}";
    }

    /// <summary>Propose alliance — only works if Friendly or better.</summary>
    public static string ProposeAlliance(Faction faction, double faithCost, Player player)
    {
        if (faction.PlayerRelation < FactionRelation.Friendly)
            return $"«{faction.Name}» недостаточно дружественны для альянса.";
        if (player.FaithPoints < faithCost)
            return $"Недостаточно ОВ (нужно {faithCost:F0}).";

        player.FaithPoints -= faithCost;
        faction.PlayerRelation = FactionRelation.Ally;
        return $"Альянс с «{faction.Name}» заключён! -{faithCost:F0} ОВ";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static FactionRelation Clamp(int value)
        => (FactionRelation)Math.Clamp(value, (int)FactionRelation.Enemy, (int)FactionRelation.Ally);
}
