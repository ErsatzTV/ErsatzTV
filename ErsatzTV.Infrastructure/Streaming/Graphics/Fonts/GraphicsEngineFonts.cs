namespace ErsatzTV.Infrastructure.Streaming.Graphics.Fonts;

public static class GraphicsEngineFonts
{
    private static readonly System.Collections.Generic.HashSet<string> LoadedFontFiles
        = new(StringComparer.OrdinalIgnoreCase);

    internal static readonly CustomFontMapper Mapper = new();

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

            using var stream = File.OpenRead(file);
            Mapper.LoadPrivateFont(stream, null);
        }
    }
}
