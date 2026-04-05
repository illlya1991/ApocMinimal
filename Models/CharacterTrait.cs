namespace ApocMinimal.Models;

/// <summary>
/// Personality trait an NPC can have (each NPC gets exactly 2).
/// </summary>
public enum CharacterTrait
{
    Brave,        // Храбрый:    бонус +10% к боевым действиям
    Cowardly,     // Трусливый:  штраф –20% к боевым действиям, но +10% к побегу
    Generous,     // Щедрый:     делится ресурсами с соседями при общении
    Greedy,       // Жадный:     требует двойную награду за квесты
    Curious,      // Любопытный: +15% к навыкам разведки и исследований
    Lazy,         // Ленивый:    –10 к дневному прогрессу задач
    Loyal,        // Преданный:  +20 к вере при работе на алтарь
    Treacherous,  // Предательский: может провалить задание намеренно
    Empathetic,   // Эмпатичный: снижает страх соседей на 5/день
    Paranoid,     // Параноидный: страх растёт на 2/день без причины
}

public static class CharacterTraitExtensions
{
    public static string ToLabel(this CharacterTrait t) => t switch
    {
        CharacterTrait.Brave       => "Храбрый",
        CharacterTrait.Cowardly    => "Трусливый",
        CharacterTrait.Generous    => "Щедрый",
        CharacterTrait.Greedy      => "Жадный",
        CharacterTrait.Curious     => "Любопытный",
        CharacterTrait.Lazy        => "Ленивый",
        CharacterTrait.Loyal       => "Преданный",
        CharacterTrait.Treacherous => "Предательский",
        CharacterTrait.Empathetic  => "Эмпатичный",
        CharacterTrait.Paranoid    => "Параноидный",
        _                          => t.ToString(),
    };

    public static CharacterTrait[] GeneratePair(Random rnd)
    {
        var all    = Enum.GetValues<CharacterTrait>();
        var first  = all[rnd.Next(all.Length)];
        CharacterTrait second;
        do { second = all[rnd.Next(all.Length)]; } while (second == first);
        return new[] { first, second };
    }
}
