namespace ApocMinimal.Extensions;

public static class CollectionExtensions
{
    /// <summary>
    /// Разбивает коллекцию на чанки указанного размера.
    /// </summary>
    public static IEnumerable<T[]> Chunk<T>(this IEnumerable<T> source, int chunkSize)
    {
        if (chunkSize <= 0)
            throw new ArgumentException("Chunk size must be positive", nameof(chunkSize));

        var list = source.ToList();
        for (int i = 0; i < list.Count; i += chunkSize)
        {
            int size = Math.Min(chunkSize, list.Count - i);
            var chunk = new T[size];
            for (int j = 0; j < size; j++)
                chunk[j] = list[i + j];
            yield return chunk;
        }
    }
}