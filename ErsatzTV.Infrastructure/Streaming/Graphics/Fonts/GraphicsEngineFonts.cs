using Topten.RichTextKit;

namespace ErsatzTV.Infrastructure.Streaming.Graphics;

public class GraphicsEngineFonts(CustomFontMapper mapper)
{
    private static readonly System.Collections.Generic.HashSet<string> LoadedFontFiles
        = new(StringComparer.OrdinalIgnoreCase);

    public FontMapper Mapper => mapper;

    public void LoadFonts(string fontsFolder)
    {
        foreach (string file in Directory.EnumerateFiles(fontsFolder, "*.*", SearchOption.AllDirectories))
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

            using FileStream stream = File.OpenRead(file);
            mapper.LoadPrivateFont(stream, null);
        }
    }
}
