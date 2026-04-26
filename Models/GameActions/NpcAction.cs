// NpcAction.cs - переименованный файл (был GameAction.cs)

using System.Collections.Generic;

namespace ApocMinimal.Models.GameActions;

public enum ActionCategory { Basic, Special }
public enum NpcActionType { Normal, Sleep, Rest, Idle }

/// <summary>
/// Действие, которое NPC может выполнять в течение дня.
/// NPC выполняет до 23 действий в день: максимум 3 специальных, остальные базовые.
/// </summary>
public class NpcAction
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ActionCategory Category { get; set; }
    public string Description { get; set; } = "";

    /// <summary>Тип действия для расчёта восстановления выносливости.</summary>
    public NpcActionType ActionType { get; set; } = NpcActionType.Normal;

    /// <summary>Стоимость выносливости в час (только для Normal-действий).</summary>
    public double StaminaCost { get; set; }

    /// <summary>Какие потребности удовлетворяет это действие (NeedName → величина удовлетворения).</summary>
    public Dictionary<string, double> SatisfiedNeeds { get; set; } = new();

    /// <summary>Требуемые минимальные значения характеристик (StatId → minValue).</summary>
    public Dictionary<int, double> RequiredStats { get; set; } = new();

    /// <summary>Характеристики, которые растут от этого действия (номера 1-30), с базовым приростом.</summary>
    public Dictionary<int, double> StatGrowthIds { get; set; } = new();

    /// <summary>Resources consumed from community pool per action (ResourceName → amount).</summary>
    public Dictionary<string, double> ResourceConsumes { get; set; } = new();
    /// <summary>Resources found via location nodes (ResourceName → base amount per action).</summary>
    public Dictionary<string, double> ResourceFinds { get; set; } = new();
}

/// <summary>
/// Каталог всех возможных действий NPC (базовые и специальные).
/// </summary>
public static class NpcActionCatalog
{
    // ── 30 БАЗОВЫХ ДЕЙСТВИЙ ────────────────────────────────────────────────────

    public static readonly NpcAction[] Basic =
    {
        new NpcAction { Id=1,  Name="Добыть еду",         Category=ActionCategory.Basic, StaminaCost=15,
                Description="Ищет пищу на улице или в зданиях.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Еда"] = 25 },
                StatGrowthIds=new Dictionary<int, double> { [1]=0.5, [3]=0.4, [6]=0.3 },
                ResourceFinds=new Dictionary<string, double> { ["Еда"] = 3 } },                    // Выносливость, Сила, Ловкость
        new NpcAction { Id=2,  Name="Набрать воды",        Category=ActionCategory.Basic, StaminaCost=10,
                Description="Собирает питьевую воду.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Вода"] = 30 },
                StatGrowthIds=new Dictionary<int, double> { [1]=0.3, [7]=0.2 },
                ResourceFinds=new Dictionary<string, double> { ["Вода"] = 5 } },                   // Выносливость, Адаптация
        new NpcAction { Id=3,  Name="Поспать",             Category=ActionCategory.Basic, StaminaCost=0, ActionType=NpcActionType.Sleep,
                Description="Восстанавливает силы и закрывает потребность во сне.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Сон"] = 12.5, ["Отдых и здоровье"] = 5 },
                StatGrowthIds=new Dictionary<int, double> { [24]=0.4, [8]=0.3 } },                 // Восстановление энергии, Регенерация
        new NpcAction { Id=4,  Name="Разжечь огонь",       Category=ActionCategory.Basic, StaminaCost=8,
                Description="Добывает тепло для группы.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Тепло"] = 35 },
                StatGrowthIds=new Dictionary<int, double> { [7]=0.3, [21]=0.2 },
                ResourceConsumes=new Dictionary<string, double> { ["Дерево"] = 2 } },              // Адаптация, Творчество
        new NpcAction { Id=5,  Name="Умыться",             Category=ActionCategory.Basic, StaminaCost=5,
                Description="Базовая гигиена.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Гигиена"] = 30 } },
        new NpcAction { Id=6,  Name="Посетить туалет",     Category=ActionCategory.Basic, StaminaCost=2,
                Description="Удовлетворяет физиологическую нужду.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Гигиена"] = 25 } },
        new NpcAction { Id=7,  Name="Осмотреться",         Category=ActionCategory.Basic, StaminaCost=5,
                Description="Проверяет периметр, снижает тревогу.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Безопасность"] = 20 },
                StatGrowthIds=new Dictionary<int, double> { [9]=0.3, [19]=0.3 } },                 // Сенсорика, Интуиция
        new NpcAction { Id=8,  Name="Отдохнуть",           Category=ActionCategory.Basic, StaminaCost=0, ActionType=NpcActionType.Rest,
                Description="Короткий отдых без сна.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Отдых и здоровье"] = 30 },
                StatGrowthIds=new Dictionary<int, double> { [24]=0.2, [4]=0.2 } },                 // Восстановление энергии, Восстановление физ
        new NpcAction { Id=9,  Name="Лечиться",            Category=ActionCategory.Basic, StaminaCost=5,
                Description="Использует медикаменты для восстановления здоровья.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Отдых и здоровье"] = 25 },
                StatGrowthIds=new Dictionary<int, double> { [8]=0.4, [4]=0.3 },
                ResourceConsumes=new Dictionary<string, double> { ["Медикаменты"] = 1 } },         // Регенерация, Восстановление физ
        new NpcAction { Id=10, Name="Поговорить",          Category=ActionCategory.Basic, StaminaCost=5,
                Description="Общение с другими выжившими.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Общение"] = 30 },
                StatGrowthIds=new Dictionary<int, double> { [20]=0.5, [18]=0.3 } },                // Социальный интеллект, Гибкость
        new NpcAction { Id=11, Name="Собрать дрова",       Category=ActionCategory.Basic, StaminaCost=12,
                Description="Заготавливает древесное топливо.",
                StatGrowthIds=new Dictionary<int, double> { [3]=0.4, [1]=0.3 },
                ResourceFinds=new Dictionary<string, double> { ["Дерево"] = 4 } },                 // Сила, Выносливость
        new NpcAction { Id=12, Name="Обыскать здание",     Category=ActionCategory.Basic, StaminaCost=18,
                Description="Ищет ресурсы в постройках.",
                StatGrowthIds=new Dictionary<int, double> { [9]=0.4, [6]=0.3, [7]=0.3 } },         // Сенсорика, Ловкость, Адаптация
        new NpcAction { Id=13, Name="Починить снаряжение", Category=ActionCategory.Basic, StaminaCost=10,
                Description="Восстанавливает инструменты и оружие.",
                StatGrowthIds=new Dictionary<int, double> { [21]=0.4, [11]=0.3 } },                // Творчество, Фокус
        new NpcAction { Id=14, Name="Приготовить пищу",    Category=ActionCategory.Basic, StaminaCost=8,
                Description="Улучшает еду: повышает её сытность.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Еда"] = 10 },
                StatGrowthIds=new Dictionary<int, double> { [21]=0.3, [18]=0.2 },
                ResourceConsumes=new Dictionary<string, double> { ["Еда"] = 1 } },                 // Творчество, Гибкость
        new NpcAction { Id=15, Name="Построить укрытие",   Category=ActionCategory.Basic, StaminaCost=20,
                Description="Возводит временную защиту.",
                StatGrowthIds=new Dictionary<int, double> { [3]=0.5, [1]=0.4, [21]=0.3 } },        // Сила, Выносливость, Творчество
        new NpcAction { Id=16, Name="Нести охрану",        Category=ActionCategory.Basic, StaminaCost=10,
                Description="Дежурит на посту, снижает риск нападения.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Безопасность"] = 15 },
                StatGrowthIds=new Dictionary<int, double> { [5]=0.4, [9]=0.4, [16]=0.3 } },        // Рефлексы, Сенсорика, Воля
        new NpcAction { Id=17, Name="Изучить территорию",  Category=ActionCategory.Basic, StaminaCost=14,
                Description="Разведывает соседние локации.",
                RequiredStats=new Dictionary<int, double> { [4] = 30 },
                StatGrowthIds=new Dictionary<int, double> { [9]=0.5, [6]=0.4, [7]=0.3 } },         // Сенсорика, Ловкость, Адаптация
        new NpcAction { Id=18, Name="Обменяться",          Category=ActionCategory.Basic, StaminaCost=5,
                Description="Торгует ресурсами с другим NPC.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Общение"] = 10 },
                StatGrowthIds=new Dictionary<int, double> { [20]=0.3, [14]=0.3 } },                // Социальный интеллект, Дедукция
        new NpcAction { Id=19, Name="Тренироваться",       Category=ActionCategory.Basic, StaminaCost=20,
                Description="Физические упражнения, развивает стат Сила/Выносливость.",
                StatGrowthIds=new Dictionary<int, double> { [3]=0.6, [1]=0.6, [2]=0.4 } },         // Сила, Выносливость, Стойкость
        new NpcAction { Id=20, Name="Записать заметки",    Category=ActionCategory.Basic, StaminaCost=3,
                Description="Документирует события в памяти.",
                StatGrowthIds=new Dictionary<int, double> { [12]=0.4, [11]=0.3 } },                // Память, Фокус
        new NpcAction { Id=21, Name="Починить крышу",      Category=ActionCategory.Basic, StaminaCost=15,
                Description="Улучшает жилищные условия, снижает потребность в Тепле.",
                StatGrowthIds=new Dictionary<int, double> { [3]=0.4, [21]=0.3 } },                 // Сила, Творчество
        new NpcAction { Id=22, Name="Убраться",            Category=ActionCategory.Basic, StaminaCost=8,
                Description="Санитарная обработка места жительства.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Гигиена"] = 15 },
                StatGrowthIds=new Dictionary<int, double> { [7]=0.2 } },                            // Адаптация
        new NpcAction { Id=23, Name="Помолиться",          Category=ActionCategory.Basic, StaminaCost=3,
                Description="Повышает Веру на небольшую величину.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Безопасность"] = 10 },
                StatGrowthIds=new Dictionary<int, double> { [16]=0.3, [26]=0.2 } },                // Воля, Концентрация
        new NpcAction { Id=24, Name="Учиться",             Category=ActionCategory.Basic, StaminaCost=6,
                Description="Читает, слушает опытных, развивает Память/Логику.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Самосовершенствование"] = 20 },
                StatGrowthIds=new Dictionary<int, double> { [15]=0.5, [12]=0.5, [17]=0.4 } },      // Интеллект, Память, Обучение
        new NpcAction { Id=25, Name="Перевязать рану",     Category=ActionCategory.Basic, StaminaCost=5,
                Description="Быстрая медицинская помощь.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Отдых и здоровье"] = 15 },
                StatGrowthIds=new Dictionary<int, double> { [8]=0.3, [4]=0.2 } },                  // Регенерация, Восстановление физ
        new NpcAction { Id=26, Name="Наблюдать",           Category=ActionCategory.Basic, StaminaCost=4,
                Description="Следит за окрестностями, пополняет память.",
                StatGrowthIds=new Dictionary<int, double> { [9]=0.4, [19]=0.3, [11]=0.2 } },       // Сенсорика, Интуиция, Фокус
        new NpcAction { Id=27, Name="Раздобыть лекарства", Category=ActionCategory.Basic, StaminaCost=16,
                Description="Ищет медикаменты.",
                StatGrowthIds=new Dictionary<int, double> { [6]=0.3, [9]=0.3, [7]=0.3 },
                ResourceFinds=new Dictionary<string, double> { ["Медикаменты"] = 2 } },            // Ловкость, Сенсорика, Адаптация
        new NpcAction { Id=28, Name="Поиграть",            Category=ActionCategory.Basic, StaminaCost=5,
                Description="Развлечение, снижает напряжение.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Отдых и здоровье"] = 15 },
                StatGrowthIds=new Dictionary<int, double> { [21]=0.2, [18]=0.2 } },                // Творчество, Гибкость
        new NpcAction { Id=29, Name="Помочь соседу",       Category=ActionCategory.Basic, StaminaCost=10,
                Description="Оказывает помощь другому NPC, повышает Доверие.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Общение"] = 15 },
                StatGrowthIds=new Dictionary<int, double> { [20]=0.4, [18]=0.3 } },                // Социальный интеллект, Гибкость
        new NpcAction { Id=30, Name="Патрулировать",       Category=ActionCategory.Basic, StaminaCost=12,
                Description="Охраняет территорию.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Безопасность"] = 20 },
                StatGrowthIds=new Dictionary<int, double> { [5]=0.4, [1]=0.3, [9]=0.3 } },         // Рефлексы, Выносливость, Сенсорика
    };

    // ── 30 СПЕЦИАЛЬНЫХ ДЕЙСТВИЙ (требуют специализации или высоких статов) ───────

    public static readonly NpcAction[] Special =
    {
        new NpcAction { Id=101, Name="Зварить самогон",       Category=ActionCategory.Special, StaminaCost=10,
                Description="Производит алкоголь.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Сомелье"] = 40 },
                RequiredStats=new Dictionary<int, double> { [18] = 40 } },
        new NpcAction { Id=102, Name="Обустроить комфорт",    Category=ActionCategory.Special, StaminaCost=12,
                Description="Улучшает бытовые условия.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Гедонист"] = 35, ["Отдых и здоровье"] = 10 } },
        new NpcAction { Id=103, Name="Организовать развлечение",Category=ActionCategory.Special, StaminaCost=8,
                Description="Устраивает досуг для группы.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Гедонист"] = 25, ["Общение"] = 15 } },
        new NpcAction { Id=104, Name="Медитировать",          Category=ActionCategory.Special, StaminaCost=0, ActionType=NpcActionType.Rest,
                Description="Глубокое расслабление, восстанавливает энергию.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Медитация"] = 50 },
                RequiredStats=new Dictionary<int, double> { [21] = 35 },
                StatGrowthIds=new Dictionary<int, double> { [26]=0.6, [25]=0.5, [23]=0.4 } },      // Концентрация, Контроль, Запас энергии
        new NpcAction { Id=105, Name="Тренировка боевых навыков",Category=ActionCategory.Special, StaminaCost=25,
                Description="Развивает боевые статы.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Спорт"] = 40 },
                RequiredStats=new Dictionary<int, double> { [1] = 40, [3] = 40 },
                StatGrowthIds=new Dictionary<int, double> { [3]=0.7, [1]=0.7, [5]=0.5, [6]=0.4 } }, // Сила, Выносливость, Рефлексы, Ловкость
        new NpcAction { Id=106, Name="Играть на инструменте", Category=ActionCategory.Special, StaminaCost=5,
                Description="Музыкальное выступление, поднимает мораль.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Музыка"] = 50 } },
        new NpcAction { Id=107, Name="Рисовать",              Category=ActionCategory.Special, StaminaCost=4,
                Description="Творческое самовыражение.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Рисование"] = 50 },
                RequiredStats=new Dictionary<int, double> { [18] = 30 } },
        new NpcAction { Id=108, Name="Готовить изысканно",    Category=ActionCategory.Special, StaminaCost=10,
                Description="Кулинарный эксперимент, улучшает качество пищи.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Гурман"] = 45, ["Еда"] = 15 },
                RequiredStats=new Dictionary<int, double> { [18] = 25 } },
        new NpcAction { Id=109, Name="Ухаживать за растениями",Category=ActionCategory.Special, StaminaCost=8,
                Description="Садоводство, создаёт возобновляемый источник еды.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Садоводство"] = 50 } },
        new NpcAction { Id=110, Name="Рыбачить",              Category=ActionCategory.Special, StaminaCost=10,
                Description="Добывает рыбу, удовлетворяет еду и хобби.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Рыбалка"] = 50, ["Еда"] = 10 },
                ResourceFinds=new Dictionary<string, double> { ["Еда"] = 4 } },
        new NpcAction { Id=111, Name="Охотиться",             Category=ActionCategory.Special, StaminaCost=20,
                Description="Охота на зверя.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Охота"] = 50, ["Еда"] = 20 },
                RequiredStats=new Dictionary<int, double> { [1] = 35, [2] = 35 },
                ResourceFinds=new Dictionary<string, double> { ["Еда"] = 8 } },
        new NpcAction { Id=112, Name="Коллекционировать",     Category=ActionCategory.Special, StaminaCost=8,
                Description="Собирает предметы, удовлетворяет увлечение.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Коллекционирование"] = 45 } },
        new NpcAction { Id=113, Name="Исследовать развалины",  Category=ActionCategory.Special, StaminaCost=20,
                Description="Экспедиция в опасные зоны.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Путешествия"] = 30, ["Адреналин"] = 25 },
                RequiredStats=new Dictionary<int, double> { [3] = 40, [9] = 35 } },
        new NpcAction { Id=114, Name="Рискованная вылазка",   Category=ActionCategory.Special, StaminaCost=22,
                Description="Экстремально опасная операция.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Адреналин"] = 55 },
                RequiredStats=new Dictionary<int, double> { [7] = 45 } },
        new NpcAction { Id=115, Name="Выступить перед группой",Category=ActionCategory.Special, StaminaCost=7,
                Description="Речь или демонстрация, повышает Признание.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Светский лев"] = 50, ["Общение"] = 15 },
                RequiredStats=new Dictionary<int, double> { [14] = 40 } },
        new NpcAction { Id=116, Name="Побыть в одиночестве",  Category=ActionCategory.Special, StaminaCost=0, ActionType=NpcActionType.Idle,
                Description="Восстановление без контакта с людьми.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Одиночество"] = 55 } },
        new NpcAction { Id=117, Name="Сыграть в карты",       Category=ActionCategory.Special, StaminaCost=5,
                Description="Азартная игра.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Азартные игры"] = 50 } },
        new NpcAction { Id=118, Name="Провести ритуал",       Category=ActionCategory.Special, StaminaCost=8,
                Description="Религиозная или мистическая практика.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Ритуалы"] = 50 },
                RequiredStats=new Dictionary<int, double> { [17] = 35 } },
        new NpcAction { Id=119, Name="Наставлять новичка",    Category=ActionCategory.Special, StaminaCost=10,
                Description="Обучает другого NPC, повышает Доверие и Признание.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Перфекционист"] = 30, ["Самосовершенствование"] = 20 },
                RequiredStats=new Dictionary<int, double> { [22] = 40 } },
        new NpcAction { Id=120, Name="Провести исследование", Category=ActionCategory.Special, StaminaCost=8,
                Description="Научный или технический анализ.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Перфекционист"] = 35, ["Самосовершенствование"] = 25 },
                RequiredStats=new Dictionary<int, double> { [12] = 40, [20] = 35 },
                StatGrowthIds=new Dictionary<int, double> { [15]=0.6, [13]=0.5, [22]=0.5, [14]=0.4 } }, // Интеллект, Логика, Математика, Дедукция
        new NpcAction { Id=121, Name="Танцевать",             Category=ActionCategory.Special, StaminaCost=10,
                Description="Танец, поднимает настроение и снижает страх.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Танцы"] = 50 } },
        new NpcAction { Id=122, Name="Петь",                  Category=ActionCategory.Special, StaminaCost=5,
                Description="Пение в одиночку или для группы.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Пение"] = 50 } },
        new NpcAction { Id=123, Name="Мастерить",             Category=ActionCategory.Special, StaminaCost=15,
                Description="Создаёт предметы ручной работы.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Ремесло"] = 50 },
                RequiredStats=new Dictionary<int, double> { [2] = 35 } },
        new NpcAction { Id=124, Name="Синтезировать вещество",Category=ActionCategory.Special, StaminaCost=12,
                Description="Химическое производство (лекарства, взрывчатка).",
                SatisfiedNeeds=new Dictionary<string, double> { ["Химия"] = 50 },
                RequiredStats=new Dictionary<int, double> { [12] = 50, [20] = 45 } },
        new NpcAction { Id=125, Name="Составить гороскоп",    Category=ActionCategory.Special, StaminaCost=4,
                Description="Астрологическая практика, снижает тревогу у суеверных.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Астрология"] = 50 },
                RequiredStats=new Dictionary<int, double> { [17] = 30 } },
        new NpcAction { Id=126, Name="Починить технику",      Category=ActionCategory.Special, StaminaCost=14,
                Description="Ремонт электронных и механических устройств.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Техника"] = 45 },
                RequiredStats=new Dictionary<int, double> { [11] = 40, [26] = 35 } },
        new NpcAction { Id=127, Name="Прочитать книгу",       Category=ActionCategory.Special, StaminaCost=3,
                Description="Интеллектуальное чтение.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Самосовершенствование"] = 20, ["Перфекционист"] = 25 },
                RequiredStats=new Dictionary<int, double> { [13] = 25 } },
        new NpcAction { Id=128, Name="Шопинг/Мародёрство",   Category=ActionCategory.Special, StaminaCost=14,
                Description="Поиск товаров в заброшенных магазинах.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Шопинг"] = 50 } },
        new NpcAction { Id=129, Name="Навестить парикмахера", Category=ActionCategory.Special, StaminaCost=3,
                Description="Уход за внешностью, повышает уверенность.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Эстет"] = 50, ["Гигиена"] = 10 } },
        new NpcAction { Id=130, Name="Заниматься сексом",     Category=ActionCategory.Special, StaminaCost=10,
                Description="Интимная близость с партнёром.",
                SatisfiedNeeds=new Dictionary<string, double> { ["Секс/Семья"] = 40, ["Романтик"] = 30 } },
    };
}