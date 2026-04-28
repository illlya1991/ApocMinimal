using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;

namespace ApocMinimal.Systems;

public enum DialogContext
{
    Greeting,
    LowFood,
    LowWater,
    QuestComplete,
    QuestFailed,
    Injury,
    LowFaith,
    HighFaith,
    Combat,
    Death,
    NewDay,
    PlayerAction,
}

public class DialogLine
{
    public string Text { get; set; } = "";
    public string NpcName { get; set; } = "";
    public string Color { get; set; } = "#c9d1d9";
}

/// <summary>
/// Procedural NPC dialog generation.
/// Context-aware lines shaped by NPC personality (traits, profession, emotions).
/// </summary>
public static class DialogSystem
{
    // ── Dialog tables by context ──────────────────────────────────────────────

    private static readonly Dictionary<DialogContext, string[]> _lines = new()
    {
        [DialogContext.Greeting] = new[]
        {
            "Ещё один день. Держимся.",
            "Как дела? Нашёл что-нибудь полезное?",
            "Не могу привыкнуть к этой тишине.",
            "Видел что-то странное сегодня ночью.",
            "Доброе утро. Хотя «доброго» в этом мало.",
            "Живём. Это главное.",
            "Сколько ещё так продолжается?",
            "Смотрел на звёзды вчера. Красиво, когда нет огней города.",
        },
        [DialogContext.LowFood] = new[]
        {
            "Живот сводит. Когда нормально поедим?",
            "Нашёл банку консервов, но этого мало на всех.",
            "Если не найдём еды — кто-то не доживёт до завтра.",
            "Голод туманит голову. Надо что-то делать.",
            "Я уже забыл, каково это — быть сытым.",
            "Послушай, нам срочно нужны продукты.",
        },
        [DialogContext.LowWater] = new[]
        {
            "Жажда хуже голода. Нужна вода — любая.",
            "Фильтры почти забились. Чистой воды почти нет.",
            "Нашёл лужу, но не рискнул пить.",
            "Обезвоживание — убийца не хуже монстров.",
        },
        [DialogContext.QuestComplete] = new[]
        {
            "Выполнил! Нелегко, но справился.",
            "Задание закрыто. Что дальше?",
            "Принёс, что просили. Обещание выполнено.",
            "Тяжело было, но вот результат.",
            "Сделано. Можно выдохнуть.",
        },
        [DialogContext.QuestFailed] = new[]
        {
            "Извини. Не смог. Слишком опасно было.",
            "Провал. Не хочу говорить об этом.",
            "Обстоятельства были сильнее меня.",
            "Я пытался. Честно.",
            "В следующий раз получится.",
        },
        [DialogContext.Injury] = new[]
        {
            "Больно. Но жить буду.",
            "Рана неглубокая. Перевязал сам.",
            "Нога подводит. Буду медленнее.",
            "Голова кружится после того удара.",
            "Нужны медикаменты. Это серьёзно.",
        },
        [DialogContext.LowFaith] = new[]
        {
            "Что-то я начинаю сомневаться. В тебе, в нас, во всём.",
            "Зачем мы вообще слушаем этого «жреца»?",
            "Мне нужны доказательства, а не обещания.",
            "Доверие — не бесконечный ресурс.",
        },
        [DialogContext.HighFaith] = new[]
        {
            "Я верю в то, что ты строишь. Пойду за тобой.",
            "Сила Терминала реальна. Я чувствую её.",
            "Ради общего дела — всё что угодно.",
            "Ты знаешь, что делаешь. Веду остальных за тобой.",
            "Это что-то большее, чем просто выживание.",
        },
        [DialogContext.Combat] = new[]
        {
            "Не трогай меня!",
            "Назад!",
            "Я не сдамся!",
            "За группу!",
            "Держись!",
            "Бей первым!",
        },
        [DialogContext.NewDay] = new[]
        {
            "Новый день — новый шанс.",
            "Пережили ночь. Уже хорошо.",
            "С рассветом чуть легче дышать.",
            "Сегодня должно быть лучше, чем вчера.",
            "Не знаю что нас ждёт. Но мы готовы.",
        },
        [DialogContext.PlayerAction] = new[]
        {
            "Понял. Сделаю.",
            "Ясно. Иду.",
            "Ты уверен? Ладно, выполняю.",
            "Не мой выбор, но приказ есть приказ.",
            "Есть. Вернусь с результатом.",
            "Это рискованно, но — хорошо.",
        },
    };

    // ── Personality modifiers ─────────────────────────────────────────────────

    private static readonly Dictionary<CharacterTrait, string[]> _traitPrefixes = new()
    {
        [CharacterTrait.Brave]       = new[]{"Не бойся — ", "Вперёд! ", "Без колебаний. "},
        [CharacterTrait.Cowardly]    = new[]{"Может, не стоит? ", "Осторожно… ", "Это опасно… "},
        [CharacterTrait.Generous]    = new[]{"Возьми, мне не жалко. ", "Поделимся. ", "Вместе справимся. "},
        [CharacterTrait.Greedy]      = new[]{"А что мне с этого? ", "Сначала о себе. ", "Награда должна быть больше. "},
        [CharacterTrait.Curious]     = new[]{"Интересно… ", "А что будет, если… ", "Надо изучить это. "},
        [CharacterTrait.Lazy]        = new[]{"Ладно, потом. ", "Может, кто другой? ", "Долго ждать нельзя, но… "},
        [CharacterTrait.Loyal]       = new[]{"Ты мой лидер. ", "Я за тебя. ", "Всё ради группы. "},
        [CharacterTrait.Treacherous] = new[]{"Конечно… ", "Как скажешь. ", "Ты можешь на меня рассчитывать. "},
        [CharacterTrait.Empathetic]  = new[]{"Понимаю тебя. ", "Как ты себя чувствуешь? ", "Главное — люди. "},
        [CharacterTrait.Paranoid]    = new[]{"За нами следят? ", "Этому нельзя доверять. ", "Всё слишком хорошо. "},
    };

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Generate a context-appropriate dialog line for an NPC.</summary>
    public static DialogLine Generate(Npc npc, DialogContext context, Random rnd)
    {
        if (!_lines.TryGetValue(context, out var pool))
            pool = _lines[DialogContext.Greeting];

        string base_ = pool[rnd.Next(pool.Length)];
        string prefix = "";

        // Apply personality prefix (from first trait if has one)
        if (npc.CharTraits.Count > 0)
        {
            var trait = npc.CharTraits[0];
            if (_traitPrefixes.TryGetValue(trait, out var prefixes))
                if (rnd.NextDouble() < 0.4)
                    prefix = prefixes[rnd.Next(prefixes.Length)];
        }

        // Special NpcTrait modifiers
        string modifier = npc.Trait switch
        {
            NpcTrait.Leader => rnd.NextDouble() < 0.3 ? " Держитесь вместе." : "",
            NpcTrait.Coward => rnd.NextDouble() < 0.3 ? " Только бы обошлось." : "",
            _               => "",
        };

        string color = context switch
        {
            DialogContext.Combat      => "#ef4444",
            DialogContext.LowFood or
            DialogContext.LowWater    => "#f97316",
            DialogContext.HighFaith   => "#22c55e",
            DialogContext.QuestComplete => "#86efac",
            DialogContext.QuestFailed   => "#f87171",
            DialogContext.Injury       => "#fbbf24",
            _                          => "#c9d1d9",
        };

        return new DialogLine
        {
            NpcName = npc.Name,
            Text    = prefix + base_ + modifier,
            Color   = color,
        };
    }

    /// <summary>Generate a dialog line based on the NPC's current state (auto-detect context).</summary>
    public static DialogLine GenerateAuto(Npc npc, Random rnd)
    {
        var context = DetectContext(npc);
        return Generate(npc, context, rnd);
    }

    /// <summary>Generate a short NPC greeting based on their emotion.</summary>
    public static string GenerateEmotionReaction(Npc npc, Random rnd)
    {
        if (npc.Emotions.Count == 0) return Generate(npc, DialogContext.Greeting, rnd).Text;

        var topEmotion = npc.Emotions.OrderByDescending(e => e.Percentage).First();
        string emo = topEmotion.Name;

        return emo switch
        {
            "Радость" or "Воодушевление"    => Generate(npc, DialogContext.HighFaith,   rnd).Text,
            "Тревога" or "Страх"            => Generate(npc, DialogContext.Combat,       rnd).Text,
            "Усталость" or "Апатия"         => Generate(npc, DialogContext.NewDay,       rnd).Text,
            "Надежда" or "Спокойствие"      => Generate(npc, DialogContext.NewDay,       rnd).Text,
            _                               => Generate(npc, DialogContext.Greeting,     rnd).Text,
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DialogContext DetectContext(Npc npc)
    {
        if (!npc.IsAlive) return DialogContext.Death;
        if (npc.Hunger > 80) return DialogContext.LowFood;
        if (npc.Thirst > 80) return DialogContext.LowWater;
        if (npc.Injuries.Count > 0) return DialogContext.Injury;
        if (npc.Devotion < 20) return DialogContext.LowFaith;
        if (npc.Devotion > 80) return DialogContext.HighFaith;
        return DialogContext.Greeting;
    }
}
