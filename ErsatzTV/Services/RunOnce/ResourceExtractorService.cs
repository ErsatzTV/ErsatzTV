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
        await ExtractResource(assembly, "song_album_cover_512.png", stoppingToken);
        await ExtractResource(assembly, "song_background_1.png", stoppingToken);
        await ExtractResource(assembly, "song_background_2.png", stoppingToken);
        await ExtractResource(assembly, "song_background_3.png", stoppingToken);
        await ExtractResource(assembly, "song_progress_overlay.png", stoppingToken);
        await ExtractResource(assembly, "song_progress_overlay_43.png", stoppingToken);
        await ExtractResource(assembly, "ErsatzTV.png", stoppingToken);
        await ExtractResource(assembly, "sequential-schedule.schema.json", stoppingToken);
        await ExtractResource(assembly, "sequential-schedule-import.schema.json", stoppingToken);
        await ExtractResource(assembly, "test.avs", stoppingToken);

        await ExtractFontResource(assembly, "Sen.ttf", stoppingToken);
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

        await ExtractTemplateResource(
            assembly,
            "_channel.sbntxt",
            FileSystemLayout.ChannelGuideTemplatesFolder,
            stoppingToken);

        await ExtractTemplateResource(
            assembly,
            "_movie.sbntxt",
            FileSystemLayout.ChannelGuideTemplatesFolder,
            stoppingToken);

        await ExtractTemplateResource(
            assembly,
            "_episode.sbntxt",
            FileSystemLayout.ChannelGuideTemplatesFolder,
            stoppingToken);

        await ExtractTemplateResource(
            assembly,
            "_musicVideo.sbntxt",
            FileSystemLayout.ChannelGuideTemplatesFolder,
            stoppingToken);

        await ExtractTemplateResource(
            assembly,
            "_song.sbntxt",
            FileSystemLayout.ChannelGuideTemplatesFolder,
            stoppingToken);

        await ExtractTemplateResource(
            assembly,
            "_otherVideo.sbntxt",
            FileSystemLayout.ChannelGuideTemplatesFolder,
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

        await ExtractMpegTsScriptResource(
            assembly,
            "run.sh",
            FileSystemLayout.DefaultMpegTsScriptFolder,
            stoppingToken);

        await ExtractMpegTsScriptResource(
            assembly,
            "run.bat",
            FileSystemLayout.DefaultMpegTsScriptFolder,
            stoppingToken);

        await ExtractMpegTsScriptResource(
            assembly,
            "mpegts.yml",
            FileSystemLayout.DefaultMpegTsScriptFolder,
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

    private static async Task ExtractMpegTsScriptResource(
        Assembly assembly,
        string name,
        string targetFolder,
        CancellationToken cancellationToken)
    {
        await using Stream resource = assembly.GetManifestResourceStream($"ErsatzTV.Resources.Scripts.MpegTs.{name}");
        if (resource != null)
        {
            await using FileStream fs = File.Create(Path.Combine(targetFolder, name));
            await resource.CopyToAsync(fs, cancellationToken);
        }
    }
}
