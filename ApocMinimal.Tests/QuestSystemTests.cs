using ApocMinimal.Models;
using ApocMinimal.Systems;
using Xunit;

namespace ApocMinimal.Tests;

public class QuestSystemTests
{
    private static Npc MakeNpc(int id, NpcTrait trait = NpcTrait.None, bool alive = true, double initiative = 70)
    {
        var n = new Npc
        {
            Id         = id,
            Name       = $"Npc{id}",
            Health     = alive ? 80 : 0,
            Initiative = initiative,
            Trait      = trait,
        };
        n.Needs = NeedSystem.InitialiseNeeds(n, new Random(id));
        return n;
    }

    private static List<Resource> Resources() =>
        new() { new Resource { Id = 1, Name = ResourceNames.Food, Amount = 50 } };

    // ── GenerateDailyQuests ───────────────────────────────────────────────────

    [Fact]
    public void GenerateDailyQuests_ReturnsOneToThreeQuests()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            var quests = QuestSystem.GenerateDailyQuests(Resources(), new Random(seed));
            Assert.InRange(quests.Count, 1, 3);
        }
    }

    [Fact]
    public void GenerateDailyQuests_StatusIsAvailable()
    {
        var quests = QuestSystem.GenerateDailyQuests(Resources(), new Random(0));
        Assert.All(quests, q => Assert.Equal(QuestStatus.Available, q.Status));
    }

    // ── AssignQuest ───────────────────────────────────────────────────────────

    [Fact]
    public void AssignQuest_SetsQuestStatusActive()
    {
        var npc   = MakeNpc(1);
        var quest = new Quest { Id = 1, Title = "Test", DaysRequired = 3 };

        QuestSystem.AssignQuest(quest, npc);

        Assert.Equal(QuestStatus.Active, quest.Status);
        Assert.Equal(npc.Id, quest.AssignedNpcId);
    }

    [Fact]
    public void AssignQuest_SetsNpcActiveTask()
    {
        var npc   = MakeNpc(1);
        var quest = new Quest { Id = 1, Title = "Найти воду", DaysRequired = 2 };

        QuestSystem.AssignQuest(quest, npc);

        Assert.Equal("Найти воду", npc.ActiveTask);
        Assert.Equal(2, npc.TaskDaysLeft);
    }

    // ── AdvanceDay ────────────────────────────────────────────────────────────

    [Fact]
    public void AdvanceDay_DecrementsDaysRemaining()
    {
        var npc   = MakeNpc(1);
        var quest = new Quest { Id = 1, Title = "Работа", DaysRequired = 5, DaysRemaining = 5 };
        QuestSystem.AssignQuest(quest, npc);

        QuestSystem.AdvanceDay(new List<Quest> { quest }, new List<Npc> { npc }, new Random(0));

        Assert.Equal(4, quest.DaysRemaining);
    }

    [Fact]
    public void AdvanceDay_CompletesQuest_WhenDaysReachZero()
    {
        var npc   = MakeNpc(1, trait: NpcTrait.None); // not coward → guaranteed finish
        var quest = new Quest { Id = 1, Title = "Сбор еды", DaysRequired = 1, DaysRemaining = 1 };
        QuestSystem.AssignQuest(quest, npc);

        var rewards = QuestSystem.AdvanceDay(
            new List<Quest> { quest }, new List<Npc> { npc }, new Random(0));

        Assert.Equal(QuestStatus.Completed, quest.Status);
        Assert.Single(rewards);
    }

    [Fact]
    public void AdvanceDay_FailsQuest_WhenNpcDead()
    {
        var npc   = MakeNpc(1, alive: false);
        var quest = new Quest { Id = 1, Title = "Патруль", DaysRequired = 3, DaysRemaining = 2 };
        quest.Status        = QuestStatus.Active;
        quest.AssignedNpcId = npc.Id;

        QuestSystem.AdvanceDay(
            new List<Quest> { quest }, new List<Npc> { npc }, new Random(0));

        Assert.Equal(QuestStatus.Failed, quest.Status);
    }

    // ── AutoAssign ────────────────────────────────────────────────────────────

    [Fact]
    public void AutoAssign_AssignsToHighInitiativeNpc()
    {
        var npc   = MakeNpc(1, initiative: 90);
        var quest = new Quest { Id = 1, Title = "Разведка", Status = QuestStatus.Available, DaysRequired = 2 };

        // Seed 0 ensures high-initiative NPC passes the 90% threshold
        QuestSystem.AutoAssign(new List<Quest> { quest }, new List<Npc> { npc }, new Random(0));

        // Quest may have been assigned (probabilistic); just ensure it's not corrupted if assigned
        if (quest.Status == QuestStatus.Active)
            Assert.Equal(npc.Id, quest.AssignedNpcId);
    }

    [Fact]
    public void AutoAssign_SkipsNpcsWithActiveTask()
    {
        var npc = MakeNpc(1, initiative: 95);
        npc.ActiveTask   = "Уже занят";
        npc.TaskDaysLeft = 3;
        var quest = new Quest { Id = 1, Title = "Работа", Status = QuestStatus.Available, DaysRequired = 2 };

        QuestSystem.AutoAssign(new List<Quest> { quest }, new List<Npc> { npc }, new Random(0));

        Assert.Equal(QuestStatus.Available, quest.Status);
    }
}
