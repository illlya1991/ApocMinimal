namespace ApocMinimal.Models.PersonData;

/// <summary>Раса/клан Владельца ЦС — выбирается при старте игры.</summary>
public enum PlayerFaction
{
    ElementMages,   // Архонты Стихий — маги-стихийники
    PathBlades,     // Клинки Пути — универсальные мечники
    MirrorHealers,  // Зеркальные Целители — усилители тела
    DeepSmiths,     // Глубинные Кузнецы — гномы-ремесленники
    GuardHeralds,   // Страж-Вестники — воины-маги защиты
}

public static class PlayerFactionExtensions
{
    public static string ToLabel(this PlayerFaction f) => f switch
    {
        PlayerFaction.ElementMages  => "Архонты Стихий",
        PlayerFaction.PathBlades    => "Клинки Пути",
        PlayerFaction.MirrorHealers => "Зеркальные Целители",
        PlayerFaction.DeepSmiths    => "Глубинные Кузнецы",
        PlayerFaction.GuardHeralds  => "Страж-Вестники",
        _ => f.ToString(),
    };

    public static string ToDescription(this PlayerFaction f) => f switch
    {
        PlayerFaction.ElementMages  => "Мастера Ченджери. Энергетические характеристики последователей усилены.",
        PlayerFaction.PathBlades    => "Дисциплина клинка. Физические характеристики последователей усилены.",
        PlayerFaction.MirrorHealers => "Исцеление изнутри. Регенерация и здоровье последователей усилены.",
        PlayerFaction.DeepSmiths    => "Ремесло предков. Производство ресурсов и скидка на техники.",
        PlayerFaction.GuardHeralds  => "Щит и меч. Барьер и контратака последователей усилены.",
        _ => "",
    };
}
