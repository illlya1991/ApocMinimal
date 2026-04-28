using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;

namespace ApocMinimal.Systems;

/// <summary>
/// Procedural quest generation using template substitution.
/// Templates use {Resource}, {Location}, {Amount} placeholders.
/// </summary>
public static class QuestGenerator
{
    private static int _nextId = 1000;

    // ── Template database ─────────────────────────────────────────────────────

    private static readonly QuestTemplate[] _templates =
    {
        new("Добыть {Resource}",
            "Найти и принести {Amount} единиц {Resource} для группы.",
            days: 2, opCost: 0, rewardMult: 1.0, category: "gather"),

        new("Зачистить {Location}",
            "Уничтожить всех монстров в {Location} и вернуть трофеи.",
            days: 3, opCost: 5, rewardMult: 1.5, category: "combat"),

        new("Разведать {Location}",
            "Исследовать {Location}, составить карту угроз.",
            days: 2, opCost: 5, rewardMult: 0.8, category: "explore"),

        new("Починить оборудование",
            "Восстановить рабочее состояние техники на базе.",
            days: 3, opCost: 10, rewardMult: 0.5, category: "craft"),

        new("Укрепить периметр",
            "Возвести дополнительные заграждения вокруг базы.",
            days: 4, opCost: 10, rewardMult: 0.3, category: "build"),

        new("Найти {Resource} для лечения",
            "Обыскать аптеки и склады в поисках {Resource}.",
            days: 3, opCost: 0, rewardMult: 1.2, category: "gather"),

        new("Охота на монстра",
            "Выследить и уничтожить опасного монстра в окрестностях.",
            days: 2, opCost: 15, rewardMult: 2.0, category: "combat"),

        new("Переговоры с выжившими",
            "Установить контакт с другой группой выживших.",
            days: 4, opCost: 20, rewardMult: 0.0, category: "social"),

        new("Создать запасы {Resource}",
            "Заготовить {Amount} единиц {Resource} на случай осады.",
            days: 5, opCost: 0, rewardMult: 0.8, category: "gather"),

        new("Патруль территории",
            "Обойти все контролируемые локации и устранить угрозы.",
            days: 2, opCost: 5, rewardMult: 1.0, category: "combat"),

        new("Эвакуировать выживших",
            "Найти и привести новых выживших на базу.",
            days: 3, opCost: 25, rewardMult: 0.0, category: "social"),

        new("Исследовать здание",
            "Провести полный обыск здания в поисках ресурсов.",
            days: 2, opCost: 0, rewardMult: 1.3, category: "explore"),
    };

    private static readonly string[] _locationNames =
    {
        "супермаркете", "больнице", "школе", "заводе", "торговом центре",
        "жилом доме", "офисном здании", "складе", "гаражном кооперативе",
        "подземной парковке", "канализационном туннеле", "старом парке",
    };

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Generate a set of daily quests using available resources as substitution context.
    /// </summary>
    public static List<Quest> GenerateDailyQuests(
        List<Resource> resources,
        Random rnd,
        int count = 0)
    {
        if (count <= 0) count = rnd.Next(1, 4);

        var result = new List<Quest>();
        var shuffled = _templates.OrderBy(_ => rnd.Next()).Take(count * 2).ToList();

        foreach (var template in shuffled)
        {
            if (result.Count >= count) break;

            Resource? res = resources.Count > 0
                ? resources[rnd.Next(resources.Count)]
                : null;

            string location = _locationNames[rnd.Next(_locationNames.Length)];
            int amount = rnd.Next(5, 31) * 5; // 25, 30, 35 ... 150

            string title = Substitute(template.TitleTemplate, res?.Name ?? "ресурсы", location, amount);
            string desc  = Substitute(template.DescTemplate,  res?.Name ?? "ресурсы", location, amount);

            double reward = template.Category == "combat" ? rnd.Next(10, 31) * template.RewardMult
                          : template.Category == "gather" ? amount * 0.8 * template.RewardMult
                          : 10 * template.RewardMult;

            result.Add(new Quest
            {
                Id               = _nextId++,
                Title            = title,
                Description      = desc,
                Source           = QuestSource.AI,
                Status           = QuestStatus.Available,
                DaysRequired     = template.Days,
                DaysRemaining    = template.Days,
                RewardResourceId = res?.Id ?? 0,
                RewardAmount     = Math.Round(reward),
                OPCost           = template.OPCost,
            });
        }

        return result;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Substitute(string template, string resource, string location, int amount)
        => template
            .Replace("{Resource}", resource)
            .Replace("{Location}", location)
            .Replace("{Amount}", amount.ToString());

    // ── Template model ────────────────────────────────────────────────────────

    private class QuestTemplate
    {
        public string TitleTemplate { get; }
        public string DescTemplate { get; }
        public int Days { get; }
        public double OPCost { get; }
        public double RewardMult { get; }
        public string Category { get; }

        public QuestTemplate(string title, string desc, int days,
            double opCost, double rewardMult, string category)
        {
            TitleTemplate = title;
            DescTemplate  = desc;
            Days          = days;
            OPCost        = opCost;
            RewardMult    = rewardMult;
            Category      = category;
        }
    }
}
