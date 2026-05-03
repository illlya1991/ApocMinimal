// Services/BrushCache.cs
using System.Collections.Concurrent;
using System.Windows.Media;

namespace ApocMinimal.Services;

public static class BrushCache
{
    private static readonly ConcurrentDictionary<string, SolidColorBrush> _cache = new();

    public static SolidColorBrush GetBrush(string hex)
    {
        return _cache.GetOrAdd(hex, h =>
        {
            try
            {
                return (SolidColorBrush)new BrushConverter().ConvertFromString(h)!;
            }
            catch
            {
                return new SolidColorBrush(Colors.White);
            }
        });
    }
}