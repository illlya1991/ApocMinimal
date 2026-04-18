using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;

namespace ApocMinimal.Systems;

/// <summary>
/// Procedural NPC generation. Creates a fully-initialized NPC from name/profession/description
/// tables, with stats shaped by profession and traits shaped by character description.
/// </summary>
public static class NpcGenerator
{
    // ── Name tables ───────────────────────────────────────────────────────────

    private static readonly string[] _maleNames =
    {
        "Алексей", "Дмитрий", "Иван", "Сергей", "Михаил", "Андрей", "Николай", "Владимир",
        "Артём", "Павел", "Константин", "Роман", "Евгений", "Игорь", "Антон", "Виктор",
        "Олег", "Александр", "Денис", "Тимур", "Вадим", "Руслан", "Марк", "Кирилл",
    };

    private static readonly string[] _femaleNames =
    {
        "Анна", "Мария", "Екатерина", "Наталья", "Ольга", "Татьяна", "Светлана", "Ирина",
        "Елена", "Юлия", "Людмила", "Галина", "Валентина", "Надежда", "Лариса", "Вера",
        "Алина", "Виктория", "Дарья", "Анастасия", "Полина", "Ксения", "Маргарита", "Зоя",
    };

    private static readonly string[] _surnames =
    {
        "Иванов", "Смирнов", "Кузнецов", "Попов", "Васильев", "Петров", "Соколов",
        "Михайлов", "Новиков", "Федоров", "Морозов", "Волков", "Алексеев", "Лебедев",
        "Семёнов", "Егоров", "Павлов", "Козлов", "Степанов", "Николаев",
    };

    // ── Profession tables with stat bias ─────────────────────────────────────

    private static readonly (string Profession, int[] BoostedStats)[] _professions =
    {
        ("Военный",       new[]{3, 1, 5, 6}),    // Сила, Выносливость, Рефлексы, Ловкость
        ("Врач",          new[]{8, 4, 15, 12}),   // Регенерация, Восстановление, Интеллект, Память
        ("Инженер",       new[]{15, 22, 21, 13}), // Интеллект, Математика, Творчество, Логика
        ("Учитель",       new[]{12, 17, 20, 15}), // Память, Обучение, Соц.интеллект, Интеллект
        ("Охотник",       new[]{3, 1, 9, 5}),     // Сила, Выносливость, Сенсорика, Рефлексы
        ("Строитель",     new[]{3, 2, 1, 7}),     // Сила, Стойкость, Выносливость, Адаптация
        ("Повар",         new[]{21, 19, 14, 18}), // Творчество, Интуиция, Дедукция, Гибкость
        ("Механик",       new[]{21, 13, 15, 11}), // Творчество, Логика, Интеллект, Фокус
        ("Полицейский",   new[]{5, 3, 9, 16}),   // Рефлексы, Сила, Сенсорика, Воля
        ("Фермер",        new[]{1, 7, 3, 8}),     // Выносливость, Адаптация, Сила, Регенерация
        ("Спортсмен",     new[]{3, 1, 6, 5}),     // Сила, Выносливость, Ловкость, Рефлексы
        ("Учёный",        new[]{15, 22, 13, 14}), // Интеллект, Математика, Логика, Дедукция
        ("Программист",   new[]{11, 15, 22, 12}), // Фокус, Интеллект, Математика, Память
        ("Художник",      new[]{21, 18, 19, 20}), // Творчество, Гибкость, Интуиция, Соц.интеллект
        ("Выживальщик",   new[]{7, 9, 1, 6}),     // Адаптация, Сенсорика, Выносливость, Ловкость
    };

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Generate a new random NPC with fully initialized stats, needs, and traits.</summary>
    public static Npc Generate(Random rnd, int day = 0)
    {
        var gender = rnd.NextDouble() < 0.5 ? Gender.Male : Gender.Female;
        string firstName = gender == Gender.Male
            ? _maleNames[rnd.Next(_maleNames.Length)]
            : _femaleNames[rnd.Next(_femaleNames.Length)];
        string surname = _surnames[rnd.Next(_surnames.Length)];
        string name = $"{firstName} {surname}";

        var (profession, boostedStats) = _professions[rnd.Next(_professions.Length)];
        var traits = CharacterTraitExtensions.GeneratePair(rnd);
        var trait = rnd.NextDouble() < 0.3
            ? (NpcTrait)rnd.Next(1, 4)  // Leader/Coward/Loner 30% chance
            : NpcTrait.None;

        int age = rnd.Next(18, 60);

        var desc = NpcDescriptions.All[rnd.Next(NpcDescriptions.All.Length)];
        var goal = NpcGoals.Goals[rnd.Next(NpcGoals.Goals.Length)];
        var dream = NpcGoals.Dreams[rnd.Next(NpcGoals.Dreams.Length)];
        var desire = NpcGoals.Desires[rnd.Next(NpcGoals.Desires.Length)];

        // Stats: start at 70-120 base, then boost profession-relevant ones
        var stats = new Models.StatisticsData.Statistics(rnd.Next(70, 121));
        foreach (int statId in boostedStats)
        {
            var ch = stats.GetByNumber(statId);
            if (ch != null) ch.AddDeviation(rnd.Next(10, 31));
        }

        var npc = new Npc
        {
            Name           = name,
            Age            = age,
            Gender         = gender,
            Profession     = profession,
            Description    = desc,
            Goal           = goal,
            Dream          = dream,
            Desire         = desire,
            Trait          = trait,
            CharTraits     = traits.ToList(),
            Stats          = stats,
            Health         = rnd.Next(60, 101),
            Faith          = rnd.Next(10, 51),
            Fear           = rnd.Next(5, 30),
            Trust          = rnd.Next(30, 71),
            Initiative     = rnd.Next(30, 80),
            CombatInitiative = rnd.Next(20, 70),
        };

        npc.Stamina = Math.Clamp(npc.MaxStamina * (0.5 + rnd.NextDouble() * 0.5), 0, npc.MaxStamina);
        npc.Chakra  = Math.Clamp(npc.MaxChakra  * (0.3 + rnd.NextDouble() * 0.5), 0, npc.MaxChakra);

        npc.Needs = NeedSystem.InitialiseNeeds(npc, rnd);

        // Specializations from profession
        npc.Specializations = GenerateSpecializations(profession, rnd);

        // Random emotion
        string[] positiveEmotions = { "Надежда", "Спокойствие", "Решимость" };
        string[] negativeEmotions = { "Тревога", "Страх", "Усталость" };
        var emo = rnd.NextDouble() < 0.6
            ? positiveEmotions[rnd.Next(positiveEmotions.Length)]
            : negativeEmotions[rnd.Next(negativeEmotions.Length)];
        npc.Emotions.Add(new Models.PersonData.NpcData.Emotion { Name = emo, Percentage = rnd.Next(40, 80) });

        return npc;
    }

    /// <summary>
    /// Обновляет Statistics и Needs существующего НПС (сохраняя имя/профессию/личность).
    /// </summary>
    public static void RefreshStatsAndNeeds(Npc npc, Random rnd)
    {
        var stats = new Models.StatisticsData.Statistics(rnd.Next(70, 121));
        var profEntry = Array.Find(_professions, p => p.Profession == npc.Profession);
        foreach (int statId in profEntry.BoostedStats ?? Array.Empty<int>())
        {
            var ch = stats.GetByNumber(statId);
            if (ch != null) ch.AddDeviation(rnd.Next(10, 31));
        }
        npc.Stats   = stats;
        npc.Stamina = Math.Clamp(npc.MaxStamina * (0.5 + rnd.NextDouble() * 0.5), 0, npc.MaxStamina);
        npc.Chakra  = Math.Clamp(npc.MaxChakra  * (0.3 + rnd.NextDouble() * 0.5), 0, npc.MaxChakra);
        npc.Needs   = NeedSystem.InitialiseNeeds(npc, rnd);
    }

    /// <summary>Generate a batch of NPCs.</summary>
    public static List<Npc> GenerateBatch(int count, Random rnd, int day = 0)
    {
        var result = new List<Npc>(count);
        for (int i = 0; i < count; i++) result.Add(Generate(rnd, day));
        return result;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<string> GenerateSpecializations(string profession, Random rnd)
    {
        var byProfession = new Dictionary<string, string[]>
        {
            ["Военный"]     = new[]{"Тактика", "Огнестрельное оружие", "Рукопашный бой", "Сапёрное дело"},
            ["Врач"]        = new[]{"Хирургия", "Фармакология", "Реанимация", "Психотерапия"},
            ["Инженер"]     = new[]{"Электроника", "Механика", "Строительство", "Сварка"},
            ["Учитель"]     = new[]{"Педагогика", "Риторика", "Психология", "Естественные науки"},
            ["Охотник"]     = new[]{"Следопытство", "Стрельба из лука", "Маскировка", "Охота"},
            ["Строитель"]   = new[]{"Плотницкое дело", "Кладка", "Арматура", "Сварка"},
            ["Повар"]       = new[]{"Консервация", "Кулинария", "Диетология", "Ферментация"},
            ["Механик"]     = new[]{"Авторемонт", "Сантехника", "Генераторы", "3D-печать"},
            ["Полицейский"] = new[]{"Следствие", "Стрельба", "Самозащита", "Тактика"},
            ["Фермер"]      = new[]{"Агрономия", "Животноводство", "Гидропоника", "Пчеловодство"},
            ["Спортсмен"]   = new[]{"Кроссфит", "Боевые искусства", "Акробатика", "Плавание"},
            ["Учёный"]      = new[]{"Химия", "Биология", "Физика", "Математический анализ"},
            ["Программист"] = new[]{"Алгоритмы", "Кибербезопасность", "ИИ", "Робототехника"},
            ["Художник"]    = new[]{"Живопись", "Скульптура", "Фотография", "Графический дизайн"},
            ["Выживальщик"] = new[]{"Ориентирование", "Добыча воды", "Разведение огня", "Ловушки"},
        };

        if (!byProfession.TryGetValue(profession, out var specs)) return new();
        int count = rnd.Next(1, 3);
        return specs.OrderBy(_ => rnd.Next()).Take(count).ToList();
    }
}
