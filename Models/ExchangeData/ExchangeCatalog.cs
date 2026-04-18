namespace ApocMinimal.Models.ExchangeData;

public static class ExchangeCatalog
{
    public static readonly PresidentialExchangeEntry[] All =
    {
        // ── ДЕНЬ 1: СТАТИЧНЫЕ ОБМЕНЫ ─────────────────────────────────────────────
        new() {
            Id=1, Name="Ядерный обмен", IsDay1Only=true,
            GiveText="Убрана радиация. Принцип деления атома недоступен.",
            GetText="Зона в Киеве: радиус 5 км — убийство запрещено системой.",
            StatEffects=[], NeedEffects=[], ResourceEffects=[],
        },
        new() {
            Id=2, Name="Военный обмен", IsDay1Only=true,
            GiveText="Эффективность пороха снижена в 2 раза.",
            GetText="Киевская область: бесконечное электричество и вода.",
            StatEffects=[],
            NeedEffects=[],
            ResourceEffects=[new("Вода", 9999), new("Еда", 500)],
        },
        new() {
            Id=3, Name="Биологический обмен", IsDay1Only=true,
            GiveText="Рождаемость снижена в 2 раза.",
            GetText="Вечный бафф ×3: Стойкость (иммунитет), Регенерация, Выносливость.",
            StatEffects=[new(2,3.0), new(8,3.0), new(1,3.0)],
            NeedEffects=[],
            ResourceEffects=[],
        },

        // ── СЛУЧАЙНЫЕ ОБМЕНЫ (30) ────────────────────────────────────────────────
        new() {
            Id=4, Name="Обмен сна и жажды",
            GiveText="Потребность в сне +1 уровень. Потребность в еде +1 уровень.",
            GetText="Потребность в воде −2 уровня.",
            NeedEffects=[new(3,+1), new(1,+1), new(2,-2)],
        },
        new() {
            Id=5, Name="Обмен долголетия",
            GiveText="Долголетие ×0.2 (старение в 5 раз быстрее).",
            GetText="Регенерация ×5.",
            StatEffects=[new(10,0.2), new(8,5.0)],
        },
        new() {
            Id=6, Name="Обмен усталости",
            GiveText="Все действия требуют вдвое больше выносливости (Выносливость ×0.5).",
            GetText="Потребность во сне у всех — минимальный уровень (1).",
            StatEffects=[new(1,0.5)],
            NeedEffects=[new(3,-99)],
        },
        new() {
            Id=7, Name="Обмен контроля",
            GiveText="Контроль энергии ×0.5.",
            GetText="Запас энергии ×2.",
            StatEffects=[new(25,0.5), new(23,2.0)],
        },
        new() {
            Id=8, Name="Обмен силы",
            GiveText="Сила ×0.5.",
            GetText="Выносливость ×2.",
            StatEffects=[new(3,0.5), new(1,2.0)],
        },
        new() {
            Id=9, Name="Обмен стойкости",
            GiveText="Стойкость ×0.5.",
            GetText="Регенерация ×2.",
            StatEffects=[new(2,0.5), new(8,2.0)],
        },
        new() {
            Id=10, Name="Обмен рефлексов",
            GiveText="Рефлексы ×0.5.",
            GetText="Адаптация ×2.",
            StatEffects=[new(5,0.5), new(7,2.0)],
        },
        new() {
            Id=11, Name="Обмен ловкости",
            GiveText="Ловкость ×0.5.",
            GetText="Сенсорика ×2.",
            StatEffects=[new(6,0.5), new(9,2.0)],
        },
        new() {
            Id=12, Name="Обмен восстановления",
            GiveText="Восстановление (физ) ×0.5.",
            GetText="Регенерация ×2.",
            StatEffects=[new(4,0.5), new(8,2.0)],
        },
        new() {
            Id=13, Name="Обмен выносливости",
            GiveText="Выносливость ×0.5.",
            GetText="Сила ×2.",
            StatEffects=[new(1,0.5), new(3,2.0)],
        },
        new() {
            Id=14, Name="Обмен адаптации",
            GiveText="Адаптация ×0.5.",
            GetText="Стойкость ×2.",
            StatEffects=[new(7,0.5), new(2,2.0)],
        },
        new() {
            Id=15, Name="Обмен интеллекта",
            GiveText="Интеллект ×0.5.",
            GetText="Интуиция ×2.",
            StatEffects=[new(15,0.5), new(19,2.0)],
        },
        new() {
            Id=16, Name="Обмен логики",
            GiveText="Логика ×0.5.",
            GetText="Социальный интеллект ×2.",
            StatEffects=[new(13,0.5), new(20,2.0)],
        },
        new() {
            Id=17, Name="Обмен памяти",
            GiveText="Память ×0.5.",
            GetText="Обучение ×2.",
            StatEffects=[new(12,0.5), new(17,2.0)],
        },
        new() {
            Id=18, Name="Обмен фокуса",
            GiveText="Фокус ×0.5.",
            GetText="Творчество ×2.",
            StatEffects=[new(11,0.5), new(21,2.0)],
        },
        new() {
            Id=19, Name="Обмен дедукции",
            GiveText="Дедукция ×0.5.",
            GetText="Гибкость мышления ×2.",
            StatEffects=[new(14,0.5), new(18,2.0)],
        },
        new() {
            Id=20, Name="Обмен воли",
            GiveText="Воля ×0.5.",
            GetText="Интуиция ×2.",
            StatEffects=[new(16,0.5), new(19,2.0)],
        },
        new() {
            Id=21, Name="Обмен математики",
            GiveText="Математика ×0.5.",
            GetText="Творчество ×2.",
            StatEffects=[new(22,0.5), new(21,2.0)],
        },
        new() {
            Id=22, Name="Обмен энергетического восстановления",
            GiveText="Восстановление энергии ×0.5.",
            GetText="Запас энергии ×2.",
            StatEffects=[new(24,0.5), new(23,2.0)],
        },
        new() {
            Id=23, Name="Обмен выхода энергии",
            GiveText="Контроль энергии ×0.5.",
            GetText="Выход энергии ×2.",
            StatEffects=[new(25,0.5), new(27,2.0)],
        },
        new() {
            Id=24, Name="Обмен концентрации",
            GiveText="Концентрация ×0.5.",
            GetText="Тонкость ×2.",
            StatEffects=[new(26,0.5), new(28,2.0)],
        },
        new() {
            Id=25, Name="Обмен устойчивости",
            GiveText="Устойчивость энергии ×0.5.",
            GetText="Восприятие энергии ×2.",
            StatEffects=[new(29,0.5), new(30,2.0)],
        },
        new() {
            Id=26, Name="Обмен восприятия",
            GiveText="Восприятие энергии ×0.5.",
            GetText="Устойчивость энергии ×2.",
            StatEffects=[new(30,0.5), new(29,2.0)],
        },
        new() {
            Id=27, Name="Обмен безопасности",
            GiveText="Потребность в безопасности +2 уровня.",
            GetText="Потребность в общении −2 уровня.",
            NeedEffects=[new(6,+2), new(8,-2)],
        },
        new() {
            Id=28, Name="Обмен гигиены",
            GiveText="Потребность в гигиене +2 уровня.",
            GetText="Потребность в тепле −2 уровня.",
            NeedEffects=[new(5,+2), new(4,-2)],
        },
        new() {
            Id=29, Name="Обмен отдыха",
            GiveText="Потребность в отдыхе и здоровье +2 уровня.",
            GetText="Потребность в самосовершенствовании −2 уровня.",
            NeedEffects=[new(7,+2), new(10,-2)],
        },
        new() {
            Id=30, Name="Обмен семьи",
            GiveText="Потребность в Секс/Семья +3 уровня.",
            GetText="Потребность в еде −1, воде −1, сне −1 уровень.",
            NeedEffects=[new(9,+3), new(1,-1), new(2,-1), new(3,-1)],
        },
        new() {
            Id=31, Name="Обмен совершенства",
            GiveText="Потребность в самосовершенствовании — максимальный уровень (5).",
            GetText="Потребность во сне — минимальный уровень (1).",
            NeedEffects=[new(10,+99), new(3,-99)],
        },
        new() {
            Id=32, Name="Обмен аскетизма",
            GiveText="Потребность в безопасности +2 уровня.",
            GetText="Потребность в Секс/Семья −2 уровня.",
            NeedEffects=[new(6,+2), new(9,-2)],
        },
        new() {
            Id=33, Name="Обмен общения",
            GiveText="Потребность в общении +2 уровня.",
            GetText="Потребность в еде −2 уровня.",
            NeedEffects=[new(8,+2), new(1,-2)],
        },
    };

    public static readonly int[] CriticalDays = { 1, 10, 40, 100, 350 };

    public static bool IsCriticalDay(int day) =>
        Array.IndexOf(CriticalDays, day) >= 0;

    public static List<PresidentialExchangeEntry> GetForDay(int day, IReadOnlyCollection<int> appliedIds, Random rnd)
    {
        if (day == 1)
            return All.Where(e => e.IsDay1Only && !appliedIds.Contains(e.Id)).ToList();

        var pool = All.Where(e => !e.IsDay1Only && !appliedIds.Contains(e.Id)).ToList();

        int r = rnd.Next(100);
        int count = r < 10 ? 0 : r < 35 ? 1 : r < 75 ? 2 : 3;
        count = Math.Min(count, pool.Count);

        var result = new List<PresidentialExchangeEntry>();
        while (result.Count < count && pool.Count > 0)
        {
            int idx = rnd.Next(pool.Count);
            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        return result;
    }
}
