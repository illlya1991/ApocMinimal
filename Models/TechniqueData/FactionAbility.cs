using ApocMinimal.Models.PersonData;

namespace ApocMinimal.Models.TechniqueData;

/// <summary>
/// Уникальная способность фракции игрока.
/// 25 способностей: 5 фракций × 5 уровней (2/4/6/8/10).
/// </summary>
public class FactionAbility
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public PlayerFaction Faction { get; set; }
    public int UnlockLevel { get; set; }       // 2, 4, 6, 8 или 10
    public double OPCostPerUse { get; set; }   // 0 = пассивная
}

/// <summary>
/// Каталог из 25 уникальных фракционных способностей.
/// </summary>
public static class FactionAbilityCatalog
{
    public static readonly FactionAbility[] All =
    {
        // ═══════════════════════════════════════════════════════════════════
        // МАГИ ЭЛЕМЕНТОВ (ElementMages)
        // ═══════════════════════════════════════════════════════════════════
        new()
        {
            Id = "fa_em_2", Faction = PlayerFaction.ElementMages, UnlockLevel = 2,
            Name = "Элементальный Поток",
            Description = "Техники, обучённые НПС, дают +15% к базовому эффекту. Маги элементов усиливают каждое знание.",
            OPCostPerUse = 0,
        },
        new()
        {
            Id = "fa_em_4", Faction = PlayerFaction.ElementMages, UnlockLevel = 4,
            Name = "Кристаллизация Силы",
            Description = "Каждые 5 дней один случайный НПС получает бесплатную технику из доступных на текущем уровне Терминала.",
            OPCostPerUse = 0,
        },
        new()
        {
            Id = "fa_em_6", Faction = PlayerFaction.ElementMages, UnlockLevel = 6,
            Name = "Буря Ченджери",
            Description = "За 25 ОР: нанести 40 урона всем монстрам в выбранной локации элементальной волной.",
            OPCostPerUse = 25,
        },
        new()
        {
            Id = "fa_em_8", Faction = PlayerFaction.ElementMages, UnlockLevel = 8,
            Name = "Мастер Стихий",
            Description = "Все НПС с изученными техниками получают +10 к каждому профильному стату.",
            OPCostPerUse = 0,
        },
        new()
        {
            Id = "fa_em_10", Faction = PlayerFaction.ElementMages, UnlockLevel = 10,
            Name = "Великий Элементаль",
            Description = "Терминал поглощает стихийную энергию: генерация ОР удваивается. Все техники стоят на 50% дешевле.",
            OPCostPerUse = 0,
        },

        // ═══════════════════════════════════════════════════════════════════
        // КЛИНКИ ПУТИ (PathBlades)
        // ═══════════════════════════════════════════════════════════════════
        new()
        {
            Id = "fa_pb_2", Faction = PlayerFaction.PathBlades, UnlockLevel = 2,
            Name = "Боевая Выправка",
            Description = "НПС с боевыми специализациями получают +5 к силе и рефлексам. Квесты на зачистку выполняются на 1 день быстрее.",
            OPCostPerUse = 0,
        },
        new()
        {
            Id = "fa_pb_4", Faction = PlayerFaction.PathBlades, UnlockLevel = 4,
            Name = "Клинок и Воля",
            Description = "После победы в бою НПС восстанавливают 15 здоровья и снижают страх на 10.",
            OPCostPerUse = 0,
        },
        new()
        {
            Id = "fa_pb_6", Faction = PlayerFaction.PathBlades, UnlockLevel = 6,
            Name = "Удар Возмездия",
            Description = "За 20 ОР: выбранный НПС атакует вражескую группу, нанося 30 урона и получая +20 инициативы на 3 дня.",
            OPCostPerUse = 20,
        },
        new()
        {
            Id = "fa_pb_8", Faction = PlayerFaction.PathBlades, UnlockLevel = 8,
            Name = "Путь Воина",
            Description = "НПС-последователи уровня 3+ получают иммунитет к страху от монстров. Боевые квесты дают двойную награду.",
            OPCostPerUse = 0,
        },
        new()
        {
            Id = "fa_pb_10", Faction = PlayerFaction.PathBlades, UnlockLevel = 10,
            Name = "Непобедимый Легион",
            Description = "Все НПС получают +20% к здоровью и +15% к урону. Монстры в защищённых зонах боятся атаковать.",
            OPCostPerUse = 0,
        },

        // ═══════════════════════════════════════════════════════════════════
        // ЗЕРКАЛЬНЫЕ ЛЕКАРИ (MirrorHealers)
        // ═══════════════════════════════════════════════════════════════════
        new()
        {
            Id = "fa_mh_2", Faction = PlayerFaction.MirrorHealers, UnlockLevel = 2,
            Name = "Отражённая Жизнь",
            Description = "Все НПС каждый день восстанавливают +3 здоровья. Травмы заживают на 1 день быстрее.",
            OPCostPerUse = 0,
        },
        new()
        {
            Id = "fa_mh_4", Faction = PlayerFaction.MirrorHealers, UnlockLevel = 4,
            Name = "Зеркало Исцеления",
            Description = "Техники лечения применённые к НПС дают двойной эффект. Преданность после лечения +5.",
            OPCostPerUse = 0,
        },
        new()
        {
            Id = "fa_mh_6", Faction = PlayerFaction.MirrorHealers, UnlockLevel = 6,
            Name = "Волна Восстановления",
            Description = "За 20 ОР: восстановить всем выжившим по 25 здоровья и снять все временные дебаффы.",
            OPCostPerUse = 20,
        },
        new()
        {
            Id = "fa_mh_8", Faction = PlayerFaction.MirrorHealers, UnlockLevel = 8,
            Name = "Зеркальный Щит",
            Description = "НПС с > 70 здоровья отражают 20% урона обратно атакующим. Смерть НПС от болезней исключена.",
            OPCostPerUse = 0,
        },
        new()
        {
            Id = "fa_mh_10", Faction = PlayerFaction.MirrorHealers, UnlockLevel = 10,
            Name = "Нетленное Зеркало",
            Description = "Выжившие не умирают от голода или жажды — они теряют здоровье, но выживают с 1 ОЗ. ОР за исцеление удваиваются.",
            OPCostPerUse = 0,
        },

        // ═══════════════════════════════════════════════════════════════════
        // ГЛУБИННЫЕ КУЗНЕЦЫ (DeepSmiths)
        // ═══════════════════════════════════════════════════════════════════
        new()
        {
            Id = "fa_ds_2", Faction = PlayerFaction.DeepSmiths, UnlockLevel = 2,
            Name = "Глубинная Добыча",
            Description = "Количество добываемых ресурсов за квесты +25%. Медикаменты и инструменты удваиваются.",
            OPCostPerUse = 0,
        },
        new()
        {
            Id = "fa_ds_4", Faction = PlayerFaction.DeepSmiths, UnlockLevel = 4,
            Name = "Мастерская Кузни",
            Description = "Каждые 3 дня в запасах появляется случайный редкий ресурс. Стоимость защиты локаций -30%.",
            OPCostPerUse = 0,
        },
        new()
        {
            Id = "fa_ds_6", Faction = PlayerFaction.DeepSmiths, UnlockLevel = 6,
            Name = "Стальная Крепость",
            Description = "За 15 ОР: укрепить одну локацию — она получает -50% урона от монстров на 7 дней.",
            OPCostPerUse = 15,
        },
        new()
        {
            Id = "fa_ds_8", Faction = PlayerFaction.DeepSmiths, UnlockLevel = 8,
            Name = "Дух Кузнеца",
            Description = "НПС-строители и добытчики работают в 2 раза эффективнее. Ресурсы не расходуются при неудачных квестах.",
            OPCostPerUse = 0,
        },
        new()
        {
            Id = "fa_ds_10", Faction = PlayerFaction.DeepSmiths, UnlockLevel = 10,
            Name = "Несокрушимая Крепость",
            Description = "Все защищённые локации неуязвимы для монстров. Ресурсов в запасах всегда минимум 20 ед. каждого типа.",
            OPCostPerUse = 0,
        },

        // ═══════════════════════════════════════════════════════════════════
        // СТРАЖИ-ВЕСТНИКИ (GuardHeralds)
        // ═══════════════════════════════════════════════════════════════════
        new()
        {
            Id = "fa_gh_2", Faction = PlayerFaction.GuardHeralds, UnlockLevel = 2,
            Name = "Весть Порядка",
            Description = "Квесты на переговоры и социальные задания всегда успешны. Доверие НПС +3 каждый день.",
            OPCostPerUse = 0,
        },
        new()
        {
            Id = "fa_gh_4", Faction = PlayerFaction.GuardHeralds, UnlockLevel = 4,
            Name = "Связь Последователей",
            Description = "НПС-последователи уровня 2+ каждый день генерируют +2 ОР дополнительно к обычному доходу.",
            OPCostPerUse = 0,
        },
        new()
        {
            Id = "fa_gh_6", Faction = PlayerFaction.GuardHeralds, UnlockLevel = 6,
            Name = "Призыв Союзников",
            Description = "За 30 ОР: привлечь 1–3 новых выживших к базе (случайный уровень и характеристики).",
            OPCostPerUse = 30,
        },
        new()
        {
            Id = "fa_gh_8", Faction = PlayerFaction.GuardHeralds, UnlockLevel = 8,
            Name = "Страж Истины",
            Description = "Предатели и враждебные НПС раскрываются автоматически. Понижение уровня последователей невозможно без команды.",
            OPCostPerUse = 0,
        },
        new()
        {
            Id = "fa_gh_10", Faction = PlayerFaction.GuardHeralds, UnlockLevel = 10,
            Name = "Вечный Вестник",
            Description = "Все выжившие получают + 1 уровень последователя автоматически при достижении 80 доверия. Лимиты удвоены.",
            OPCostPerUse = 0,
        },
    };

    public static FactionAbility? Find(string id)
        => All.FirstOrDefault(a => a.Id == id);

    public static IEnumerable<FactionAbility> GetForFaction(PlayerFaction faction)
        => All.Where(a => a.Faction == faction);

    public static IEnumerable<FactionAbility> GetUnlocked(PlayerFaction faction, int terminalLevel)
        => All.Where(a => a.Faction == faction && a.UnlockLevel <= terminalLevel);
}
