using ApocMinimal.Models;
using ApocMinimal.Systems;
using Xunit;

namespace ApocMinimal.Tests;

public class NeedSystemTests
{
    private static Npc MakeNpc(int id = 1) => new()
    {
        Id         = id,
        Name       = "TestNpc",
        Health     = 100,
        Stamina    = 100,
        Fear       = 10,
        FollowerLevel = 0,
    };

    // ── InitialiseNeeds ───────────────────────────────────────────────────────

    [Fact]
    public void InitialiseNeeds_Creates10BasicNeeds()
    {
        var npc = MakeNpc();
        npc.Needs = NeedSystem.InitialiseNeeds(npc, new Random(42));

        var basic = npc.Needs.Where(n => n.Category == NeedCategory.Basic).ToList();
        Assert.Equal(10, basic.Count);
    }

    [Fact]
    public void InitialiseNeeds_BasicNeedIdsMatchEnum()
    {
        var npc = MakeNpc();
        npc.Needs = NeedSystem.InitialiseNeeds(npc, new Random(1));

        Assert.Equal((int)BasicNeedId.Food,   npc.Needs.First(n => n.Name == "Еда").Id);
        Assert.Equal((int)BasicNeedId.Water,  npc.Needs.First(n => n.Name == "Вода").Id);
        Assert.Equal((int)BasicNeedId.Safety, npc.Needs.First(n => n.Name == "Безопасность").Id);
    }

    [Fact]
    public void InitialiseNeeds_AddsAtLeastOneSpecialNeed()
    {
        var npc = MakeNpc();
        npc.Needs = NeedSystem.InitialiseNeeds(npc, new Random(99));

        Assert.Contains(npc.Needs, n => n.Category == NeedCategory.Special);
    }

    // ── SatisfyNeed ───────────────────────────────────────────────────────────

    [Fact]
    public void SatisfyNeed_ByEnum_DecreasesValue()
    {
        var npc = MakeNpc();
        npc.Needs = NeedSystem.InitialiseNeeds(npc, new Random(0));
        var food = npc.Needs.First(n => n.Id == (int)BasicNeedId.Food);
        food.Value = 60;

        NeedSystem.SatisfyNeed(npc, BasicNeedId.Food, 20);

        Assert.Equal(40.0, food.Value, 1);
    }

    [Fact]
    public void SatisfyNeed_ByString_DecreasesValue()
    {
        var npc = MakeNpc();
        npc.Needs = NeedSystem.InitialiseNeeds(npc, new Random(0));
        var water = npc.Needs.First(n => n.Id == (int)BasicNeedId.Water);
        water.Value = 50;

        NeedSystem.SatisfyNeed(npc, "Вода", 15);

        Assert.Equal(35.0, water.Value, 1);
    }

    [Fact]
    public void SatisfyNeed_ReturnsFalse_WhenNeedNotFound()
    {
        var npc = MakeNpc();
        npc.Needs = NeedSystem.InitialiseNeeds(npc, new Random(0));

        bool result = NeedSystem.SatisfyNeed(npc, 999, 10);

        Assert.False(result);
    }

    [Fact]
    public void SatisfyNeed_ClampsValueToZero()
    {
        var npc = MakeNpc();
        npc.Needs = NeedSystem.InitialiseNeeds(npc, new Random(0));
        npc.Needs.First(n => n.Id == (int)BasicNeedId.Food).Value = 5;

        NeedSystem.SatisfyNeed(npc, BasicNeedId.Food, 100);

        Assert.Equal(0, npc.Needs.First(n => n.Id == (int)BasicNeedId.Food).Value);
    }

    // ── ApplyDailyDecay ───────────────────────────────────────────────────────

    [Fact]
    public void ApplyDailyDecay_IncreasesBasicNeedValues()
    {
        var npc = MakeNpc();
        npc.Needs = NeedSystem.InitialiseNeeds(npc, new Random(0));
        foreach (var n in npc.Needs) n.Value = 0;

        NeedSystem.ApplyDailyDecay(npc);

        Assert.All(npc.Needs.Where(n => n.Category == NeedCategory.Basic),
            n => Assert.True(n.Value > 0, $"{n.Name} should have decayed"));
    }

    [Fact]
    public void ApplyDailyDecay_ToiletDecaysFastest()
    {
        var npc = MakeNpc();
        npc.Needs = NeedSystem.InitialiseNeeds(npc, new Random(0));
        foreach (var n in npc.Needs) { n.Value = 0; n.Level = 1; }

        NeedSystem.ApplyDailyDecay(npc);

        var toilet = npc.Needs.First(n => n.Id == (int)BasicNeedId.Toilet);
        var health = npc.Needs.First(n => n.Id == (int)BasicNeedId.Health);
        Assert.True(toilet.Value > health.Value);
    }

    // ── ApplyPenalties ────────────────────────────────────────────────────────

    [Fact]
    public void ApplyPenalties_ReducesHealth_WhenFoodCritical()
    {
        var npc = MakeNpc();
        npc.Needs = NeedSystem.InitialiseNeeds(npc, new Random(0));
        var food = npc.Needs.First(n => n.Id == (int)BasicNeedId.Food);
        food.Value = 90;   // IsCritical = true (>= 80)
        food.Level = 3;
        npc.Health = 80;

        NeedSystem.ApplyPenalties(npc);

        Assert.True(npc.Health < 80);
    }

    [Fact]
    public void ApplyPenalties_IncreasesFear_WhenSafetyCritical()
    {
        var npc = MakeNpc();
        npc.Needs = NeedSystem.InitialiseNeeds(npc, new Random(0));
        var safety = npc.Needs.First(n => n.Id == (int)BasicNeedId.Safety);
        safety.Value = 85;
        safety.Level = 2;
        npc.Fear = 20;

        NeedSystem.ApplyPenalties(npc);

        Assert.True(npc.Fear > 20);
    }

    // ── GetMostUrgentNeed ─────────────────────────────────────────────────────

    [Fact]
    public void GetMostUrgentNeed_ReturnsHighestValueTimesLevel()
    {
        var npc = MakeNpc();
        npc.Needs = NeedSystem.InitialiseNeeds(npc, new Random(0));
        foreach (var n in npc.Needs) n.Value = 10;

        var water = npc.Needs.First(n => n.Id == (int)BasicNeedId.Water);
        water.Value = 70;
        water.Level = 5;

        var urgent = NeedSystem.GetMostUrgentNeed(npc);

        Assert.Equal(water.Id, urgent?.Id);
    }

    [Fact]
    public void GetMostUrgentNeed_ReturnsNull_WhenAllSatisfied()
    {
        var npc = MakeNpc();
        npc.Needs = NeedSystem.InitialiseNeeds(npc, new Random(0));
        foreach (var n in npc.Needs) n.Value = 5;   // all IsSatisfied (<=20)

        Assert.Null(NeedSystem.GetMostUrgentNeed(npc));
    }
}
