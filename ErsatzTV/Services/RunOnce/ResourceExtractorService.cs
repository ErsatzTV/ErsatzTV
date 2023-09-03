using System.Reflection;
using ErsatzTV.Core;

namespace ErsatzTV.Services.RunOnce;

public class ResourceExtractorService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        if (!Directory.Exists(FileSystemLayout.ResourcesCacheFolder))
        {
            Directory.CreateDirectory(FileSystemLayout.ResourcesCacheFolder);
        }

        Assembly assembly = typeof(ResourceExtractorService).GetTypeInfo().Assembly;

        await ExtractResource(assembly, "background.png", stoppingToken);
        await ExtractResource(assembly, "song_background_1.png", stoppingToken);
        await ExtractResource(assembly, "song_background_2.png", stoppingToken);
        await ExtractResource(assembly, "song_background_3.png", stoppingToken);
        await ExtractResource(assembly, "ErsatzTV.png", stoppingToken);

        await ExtractFontResource(assembly, "Roboto-Regular.ttf", stoppingToken);
        await ExtractFontResource(assembly, "OPTIKabel-Heavy.otf", stoppingToken);

        await ExtractTemplateResource(
            assembly,
            "_default.ass.sbntxt",
            FileSystemLayout.MusicVideoCreditsTemplatesFolder,
            stoppingToken);

        await ExtractTemplateResource(
            assembly,
            "_ArtistTitle_LeftMiddle.sbntxt",
            FileSystemLayout.MusicVideoCreditsTemplatesFolder,
            stoppingToken);

        await ExtractTemplateResource(
            assembly,
            "_ArtistTitleAlbum_CenterTop.sbntxt",
            FileSystemLayout.MusicVideoCreditsTemplatesFolder,
            stoppingToken);

        await ExtractScriptResource(
            assembly,
            "_threePartEpisodes.js",
            FileSystemLayout.MultiEpisodeShuffleTemplatesFolder,
            stoppingToken);

        await ExtractScriptResource(
            assembly,
            "_episode.js",
            FileSystemLayout.AudioStreamSelectorScriptsFolder,
            stoppingToken);

        await ExtractScriptResource(
            assembly,
            "_movie.js",
            FileSystemLayout.AudioStreamSelectorScriptsFolder,
            stoppingToken);
    }

    private static async Task ExtractResource(Assembly assembly, string name, CancellationToken cancellationToken)
    {
        await using Stream resource = assembly.GetManifestResourceStream($"ErsatzTV.Resources.{name}");
        if (resource != null)
        {
            await using FileStream fs = File.Create(Path.Combine(FileSystemLayout.ResourcesCacheFolder, name));
            await resource.CopyToAsync(fs, cancellationToken);
        }
    }

    private static async Task ExtractFontResource(Assembly assembly, string name, CancellationToken cancellationToken)
    {
        await using Stream resource = assembly.GetManifestResourceStream($"ErsatzTV.Resources.Fonts.{name}");
        if (resource != null)
        {
            await using FileStream fs = File.Create(Path.Combine(FileSystemLayout.ResourcesCacheFolder, name));
            await resource.CopyToAsync(fs, cancellationToken);

            resource.Position = 0;

            await using FileStream fontCacheFileStream =
                File.Create(Path.Combine(FileSystemLayout.FontsCacheFolder, name));
            await resource.CopyToAsync(fontCacheFileStream, cancellationToken);
        }
    }

    private static async Task ExtractTemplateResource(
        Assembly assembly,
        string name,
        string targetFolder,
        CancellationToken cancellationToken)
    {
        await using Stream resource = assembly.GetManifestResourceStream($"ErsatzTV.Resources.Templates.{name}");
        if (resource != null)
        {
            await using FileStream fs = File.Create(Path.Combine(targetFolder, name));
            await resource.CopyToAsync(fs, cancellationToken);
        }
    }

    private static async Task ExtractScriptResource(
        Assembly assembly,
        string name,
        string targetFolder,
        CancellationToken cancellationToken)
    {
        await using Stream resource = assembly.GetManifestResourceStream($"ErsatzTV.Resources.Scripts.{name}");
        if (resource != null)
        {
            await using FileStream fs = File.Create(Path.Combine(targetFolder, name));
            await resource.CopyToAsync(fs, cancellationToken);
        }
    }
}
