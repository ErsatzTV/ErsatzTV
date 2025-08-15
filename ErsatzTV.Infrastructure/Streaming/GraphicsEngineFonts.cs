using System.Collections.Concurrent;
using SkiaSharp;

namespace ErsatzTV.Infrastructure.Streaming;

public static class GraphicsEngineFonts
{
    private static readonly ConcurrentDictionary<string, SKTypeface> CustomTypefaces = new(StringComparer.OrdinalIgnoreCase);
    private static readonly System.Collections.Generic.HashSet<string> LoadedFontFiles
        = new(StringComparer.OrdinalIgnoreCase);

    public static void LoadFonts(string fontsFolder)
    {
        foreach (var file in Directory.EnumerateFiles(fontsFolder, "*.*", SearchOption.AllDirectories))
        {
            if (!file.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) &&
                !file.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!LoadedFontFiles.Add(file))
            {
                continue;
            }

            var typeface = SKTypeface.FromFile(file);
            if (typeface != null)
            {
                CustomTypefaces.TryAdd(typeface.FamilyName, typeface);
            }
        }
    }

    public static SKTypeface GetTypeface(string fontFamilyName)
    {
        if (CustomTypefaces.TryGetValue(fontFamilyName, out var typeface))
        {
            return typeface;
        }

        return SKFontManager.Default.MatchFamily(fontFamilyName) ?? SKTypeface.Default;
    }
}
