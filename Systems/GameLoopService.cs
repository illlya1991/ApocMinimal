// GameLoopService.cs - оптимизированная версия

using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;

namespace ApocMinimal.Systems;

public class NpcDayResult
{
    public Npc Npc { get; }
    public IReadOnlyList<ActionLogEntry> Actions { get; }

    public NpcDayResult(Npc npc, List<ActionLogEntry> actions)
    {
        Npc = npc;
        Actions = actions;
    }
}

public class DayResult
{
    public List<NpcDayResult> NpcResults { get; } = new List<NpcDayResult>();
    public List<(Npc Npc, Quest Quest)> QuestRewards { get; } = new List<(Npc, Quest)>();
    public List<Quest> NewQuests { get; } = new List<Quest>();
    public List<(string Text, bool IsAlert)> Logs { get; } = new List<(string, bool)>();
    public double FaithGained { get; set; }
    public int FollowerCount { get; set; }
}

public static class GameLoopService
{
    private static readonly string[] PositiveEmotionsArray = new string[]
    {
        "Радость", "Спокойствие", "Надежда", "Любовь",
        "Воодушевление", "Гордость", "Благодарность",
    };

    private static bool IsPositiveEmotion(string emotionName)
    {
        for (int i = 0; i < PositiveEmotionsArray.Length; i++)
        {
            if (PositiveEmotionsArray[i] == emotionName)
                return true;
        }
        return false;
    }

    public static DayResult ProcessDay(
        Player player,
        List<Npc> npcs,
        List<Resource> resources,
        List<Quest> quests,
        Random rnd)
    {
        DayResult result = new DayResult();

        List<Npc> aliveNpcs = new List<Npc>();
        for (int i = 0; i < npcs.Count; i++)
        {
            if (npcs[i].IsAlive)
                aliveNpcs.Add(npcs[i]);
        }

        for (int i = 0; i < aliveNpcs.Count; i++)
        {
            Npc npc = aliveNpcs[i];
            List<ActionLogEntry> actions = ActionSystem.ProcessDayActions(npc, rnd, player.CurrentDay);
            result.NpcResults.Add(new NpcDayResult(npc, actions));
        }

        List<(Npc, Quest)> rewards = QuestSystem.AdvanceDay(quests, npcs, rnd);
        for (int i = 0; i < rewards.Count; i++)
        {
            result.QuestRewards.Add(rewards[i]);
        }

        List<string> autoAssignLogs = QuestSystem.AutoAssign(quests, npcs, rnd);
        for (int i = 0; i < autoAssignLogs.Count; i++)
        {
            result.Logs.Add((autoAssignLogs[i], false));
        }

        List<Quest> newQuests = QuestSystem.GenerateDailyQuests(resources, rnd);
        for (int i = 0; i < newQuests.Count; i++)
        {
            result.NewQuests.Add(newQuests[i]);
            quests.Add(newQuests[i]);
        }

        for (int i = 0; i < npcs.Count; i++)
        {
            Npc leader = npcs[i];
            if (!leader.IsAlive) continue;
            if (leader.Trait != NpcTrait.Leader) continue;

            List<Npc> targets = new List<Npc>();
            for (int j = 0; j < npcs.Count; j++)
            {
                Npc target = npcs[j];
                if (target.IsAlive && target.Id != leader.Id && target.Trait != NpcTrait.Loner)
                    targets.Add(target);
            }

            for (int j = 0; j < targets.Count; j++)
            {
                Npc t = targets[j];
                double newFaith = t.Faith + 3;
                if (newFaith > 100) newFaith = 100;
                t.Faith = newFaith;
            }

            if (targets.Count > 0)
            {
                result.Logs.Add(($"{leader.Name} (Лидер) поднял Веру {targets.Count} выжившим +3", false));
            }
        }

        double faithTotal = 0;
        int followerCount = 0;

        for (int i = 0; i < npcs.Count; i++)
        {
            Npc npc = npcs[i];
            if (!npc.IsAlive) continue;
            if (npc.FollowerLevel <= 0) continue;

            followerCount++;

            double maxDay = npc.FollowerLevel * (Player.MaxFaithPerNpcPerDay / 5.0);

            double avgSat = 0.5;
            if (npc.Needs.Count > 0)
            {
                double sumSat = 0;
                for (int j = 0; j < npc.Needs.Count; j++)
                {
                    sumSat += npc.Needs[j].Satisfaction;
                }
                avgSat = (sumSat / npc.Needs.Count) / 100.0;
            }

            double trustMod = 0.3 + (npc.Trust / 100.0) * 0.7;

            double posSum = 0;
            for (int j = 0; j < npc.Emotions.Count; j++)
            {
                if (IsPositiveEmotion(npc.Emotions[j].Name))
                    posSum += npc.Emotions[j].Percentage;
            }
            double emoMod = 0.5 + posSum / 200.0;

            faithTotal += Math.Min(maxDay, maxDay * avgSat * trustMod * emoMod);
        }

        player.FaithPoints += faithTotal;
        result.FaithGained = faithTotal;
        result.FollowerCount = followerCount;

        int alive = 0;
        for (int i = 0; i < npcs.Count; i++)
        {
            if (npcs[i].IsAlive)
                alive++;
        }

        if (alive > 0)
        {
            Resource? food = null;
            Resource? water = null;

            for (int i = 0; i < resources.Count; i++)
            {
                if (resources[i].Name == ResourceNames.Food)
                    food = resources[i];
                else if (resources[i].Name == ResourceNames.Water)
                    water = resources[i];
            }

            if (food != null && food.Amount > 0)
            {
                double eat = Math.Min(food.Amount, alive);
                food.Amount -= eat;

                int eatInt = (int)eat;
                for (int i = 0; i < npcs.Count && i < eatInt; i++)
                {
                    if (npcs[i].IsAlive)
                        NeedSystem.SatisfyNeed(npcs[i], BasicNeedId.Food, 30);
                }

                result.Logs.Add(($"{ResourceNames.Food}: -{eat:F0} ед.  Осталось: {food.Amount:F0}", false));
            }
            else if (food != null && food.Amount <= 0)
            {
                result.Logs.Add(($"{ResourceNames.Food}: закончилась! Голод наносит урон!", true));
                for (int i = 0; i < npcs.Count; i++)
                {
                    if (npcs[i].IsAlive)
                    {
                        double newHealth = npcs[i].Health - 5;
                        if (newHealth < 0) newHealth = 0;
                        npcs[i].Health = newHealth;
                    }
                }
            }

            if (water != null && water.Amount > 0)
            {
                double drink = Math.Min(water.Amount, alive);
                water.Amount -= drink;

                int drinkInt = (int)drink;
                for (int i = 0; i < npcs.Count && i < drinkInt; i++)
                {
                    if (npcs[i].IsAlive)
                        NeedSystem.SatisfyNeed(npcs[i], BasicNeedId.Water, 35);
                }

                result.Logs.Add(($"{ResourceNames.Water}: -{drink:F0} ед.  Осталось: {water.Amount:F0}", false));
            }
            else if (water != null && water.Amount <= 0)
            {
                result.Logs.Add(($"{ResourceNames.Water}: закончилась! Обезвоживание наносит урон!", true));
                for (int i = 0; i < npcs.Count; i++)
                {
                    if (npcs[i].IsAlive)
                    {
                        double newHealth = npcs[i].Health - 8;
                        if (newHealth < 0) newHealth = 0;
                        npcs[i].Health = newHealth;
                    }
                }
            }
        }

        return result;
    }
}