using System.Collections.Concurrent;
using System.Globalization;
using SixLabors.Fonts;

namespace ErsatzTV.Infrastructure.Streaming;

public static class GraphicsEngineFonts
{
    private static readonly FontCollection CustomFontCollection = new();
    private static readonly ConcurrentDictionary<string, FontFamily> CustomFontFamilies
        = new(StringComparer.OrdinalIgnoreCase);
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

            var fontFamily = CustomFontCollection.Add(file, CultureInfo.CurrentCulture);
            CustomFontFamilies.TryAdd(fontFamily.Name, fontFamily);
        }
    }

    public static Font GetFont(string fontFamilyName, float fontSize, FontStyle style)
    {
        // try custom fonts
        if (CustomFontFamilies.TryGetValue(fontFamilyName, out var customFamily))
        {
            return customFamily.GetAvailableStyles().Contains(style)
                ? customFamily.CreateFont(fontSize, style)
                : customFamily.CreateFont(fontSize);
        }

        // fallback to system fonts
        if (SystemFonts.TryGet(fontFamilyName, CultureInfo.CurrentCulture, out var systemFamily))
        {
            return systemFamily.GetAvailableStyles().Contains(style)
                ? systemFamily.CreateFont(fontSize, style)
                : systemFamily.CreateFont(fontSize);
        }

        // fallback to default font
        var fallback = SystemFonts.Families.First();
        return fallback.CreateFont(fontSize, style);
    }
}
