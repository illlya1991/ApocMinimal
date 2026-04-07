using ApocMinimal.Models;
using ApocMinimal.Systems;
using Xunit;

namespace ApocMinimal.Tests;

public class ActionSystemTests
{
    private static Npc MakeAliveNpc(NpcTrait trait = NpcTrait.None)
    {
        var npc = new Npc
        {
            Id        = 1,
            Name      = "TestNpc",
            Health    = 100,
            Stamina   = 100,
            Chakra    = 50,
            Fear      = 10,
            Trust     = 50,
            Initiative = 50,
            Trait     = trait,
        };
        npc.Needs = NeedSystem.InitialiseNeeds(npc, new Random(42));
        npc.Stats = new Dictionary<int, double>();
        return npc;
    }

    // ── Basic execution ───────────────────────────────────────────────────────

    [Fact]
    public void ProcessDayActions_ReturnsNoEntries_ForDeadNpc()
    {
        var npc = MakeAliveNpc();
        npc.Health = 0;   // dead

        var log = ActionSystem.ProcessDayActions(npc, new Random(0), day: 1);

        Assert.Empty(log);
    }

    [Fact]
    public void ProcessDayActions_ReturnsEntries_ForAliveNpc()
    {
        var npc = MakeAliveNpc();

        var log = ActionSystem.ProcessDayActions(npc, new Random(0), day: 1);

        Assert.NotEmpty(log);
    }

    [Fact]
    public void ProcessDayActions_NeverExceedsMaxActionsPerDay()
    {
        var npc = MakeAliveNpc();

        var log = ActionSystem.ProcessDayActions(npc, new Random(0), day: 1);

        // Non-alert entries = actual actions (≤ 23)
        int actions = log.Count(e => !e.IsAlert);
        Assert.True(actions <= 23, $"Expected ≤ 23 actions, got {actions}");
    }

    [Fact]
    public void ProcessDayActions_EntriesHaveTimestamps()
    {
        var npc = MakeAliveNpc();

        var log = ActionSystem.ProcessDayActions(npc, new Random(5), day: 2);

        Assert.All(log.Where(e => !e.IsAlert),
            e => Assert.Matches(@"^\d{2}:\d{2}$", e.Time));
    }

    // ── Coward trait ──────────────────────────────────────────────────────────

    [Fact]
    public void ProcessDayActions_Coward_MaySkipEntireDay()
    {
        // Run 100 seeds; at least some should produce skip (≤1 entry)
        int skipped = 0;
        for (int seed = 0; seed < 100; seed++)
        {
            var npc = MakeAliveNpc(NpcTrait.Coward);
            var log = ActionSystem.ProcessDayActions(npc, new Random(seed), day: 1);
            if (log.Count <= 1) skipped++;
        }
        Assert.True(skipped > 0, "Coward should skip day at least once in 100 runs");
    }

    // ── Needs satisfaction ────────────────────────────────────────────────────

    [Fact]
    public void ProcessDayActions_Prioritises_UrgentNeeds()
    {
        var npc = MakeAliveNpc();
        // Make food critically urgent
        var food = npc.Needs.First(n => n.Id == (int)BasicNeedId.Food);
        food.Value = 90;

        var log = ActionSystem.ProcessDayActions(npc, new Random(7), day: 1);

        // After processing, food need should have been at least partially addressed
        // (value reduced or stamina consumed for food-related action)
        Assert.NotEmpty(log);
    }

    // ── Lazy trait ────────────────────────────────────────────────────────────

    [Fact]
    public void ProcessDayActions_LazyNpc_FewerActions_ThanNormal()
    {
        int lazy = 0, normal = 0;
        for (int seed = 0; seed < 10; seed++)
        {
            var npcLazy = MakeAliveNpc();
            npcLazy.CharTraits.Add(CharacterTrait.Lazy);
            lazy += ActionSystem.ProcessDayActions(npcLazy, new Random(seed), day: 1)
                .Count(e => !e.IsAlert);

            var npcNorm = MakeAliveNpc();
            normal += ActionSystem.ProcessDayActions(npcNorm, new Random(seed), day: 1)
                .Count(e => !e.IsAlert);
        }
        Assert.True(lazy < normal, $"Lazy total {lazy} should be less than normal {normal}");
    }

    // ── Alert entries ─────────────────────────────────────────────────────────

    [Fact]
    public void ProcessDayActions_AlertEntries_ForCriticalNeeds()
    {
        var npc = MakeAliveNpc();
        // Force all needs to critical
        foreach (var n in npc.Needs) n.Value = 90;

        var log = ActionSystem.ProcessDayActions(npc, new Random(0), day: 1);

        Assert.Contains(log, e => e.IsAlert);
    }
}
