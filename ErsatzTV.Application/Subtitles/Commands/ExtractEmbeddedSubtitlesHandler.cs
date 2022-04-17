using System.Security.Cryptography;
using System.Text;
using CliWrap;
using CliWrap.Buffered;
using CliWrap.Builders;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Subtitles;

public class ExtractEmbeddedSubtitlesHandler : IRequestHandler<ExtractEmbeddedSubtitles, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger<ExtractEmbeddedSubtitlesHandler> _logger;

    public ExtractEmbeddedSubtitlesHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        ILocalFileSystem localFileSystem,
        ILogger<ExtractEmbeddedSubtitlesHandler> logger)
    {
        _dbContextFactory = dbContextFactory;
        _localFileSystem = localFileSystem;
        _logger = logger;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        ExtractEmbeddedSubtitles request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, string> validation = await FFmpegPathMustExist(dbContext);
        return await validation.Match(
            ffmpegPath => ExtractAll(dbContext, request, ffmpegPath, cancellationToken),
            error => Task.FromResult<Either<BaseError, Unit>>(error.Join()));
    }

    private async Task<Either<BaseError, Unit>> ExtractAll(
        TvContext dbContext,
        ExtractEmbeddedSubtitles request,
        string ffmpegPath,
        CancellationToken cancellationToken)
    {
        try
        {
            DateTime now = DateTime.UtcNow;
            DateTime until = now.AddHours(1);

            // check the requested playout if one was passed
            var playoutIdsToCheck = new List<int>();
            playoutIdsToCheck.AddRange(request.PlayoutId);

            // check all playouts if none were passed
            if (playoutIdsToCheck.Count == 0)
            {
                playoutIdsToCheck = dbContext.Playouts.Map(p => p.Id).ToList();
            }

            // find all playout items in the next hour
            List<PlayoutItem> playoutItems = await dbContext.PlayoutItems
                .Filter(pi => playoutIdsToCheck.Contains(pi.PlayoutId))
                .Filter(pi => pi.Start <= until)
                .ToListAsync(cancellationToken);

            // TODO: support other media kinds (movies, other videos, etc)

            var mediaItemIds = playoutItems.Map(pi => pi.MediaItemId).ToList();

            // filter for subtitles that need extraction
            List<int> episodeIds = await dbContext.EpisodeMetadata
                .Filter(em => mediaItemIds.Contains(em.EpisodeId))
                .Filter(
                    em => em.Subtitles.Any(
                        s => s.SubtitleKind == SubtitleKind.Embedded && s.IsExtracted == false &&
                             s.Codec != "hdmv_pgs_subtitle" && s.Codec != "dvd_subtitle"))
                .Map(em => em.EpisodeId)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Episode ids to update {EpisodeIds}", episodeIds);

            // sort by start time
            var toUpdate = playoutItems
                .DistinctBy(pi => pi.MediaItemId)
                .Filter(pi => episodeIds.Contains(pi.MediaItemId))
                .OrderBy(pi => pi.StartOffset)
                .Map(pi => pi.MediaItemId)
                .ToList();

            foreach (int episodeId in toUpdate)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return Unit.Default;
                }

                // extract subtitles and fonts for each item and update db
                await ExtractSubtitles(dbContext, episodeId, ffmpegPath, cancellationToken);
                // await ExtractFonts(dbContext, episodeId, ffmpegPath, cancellationToken);
            }

            return Unit.Default;
        }
        catch (TaskCanceledException)
        {
            return Unit.Default;
        }
    }

    private async Task<Unit> ExtractSubtitles(
        TvContext dbContext,
        int mediaItemId,
        string ffmpegPath,
        CancellationToken cancellationToken)
    {
        Option<Episode> maybeEpisode = await dbContext.Episodes
            .Include(e => e.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(e => e.MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(e => e.EpisodeMetadata)
            .ThenInclude(em => em.Subtitles)
            .SelectOneAsync(e => e.Id, e => e.Id == mediaItemId);

        foreach (Episode episode in maybeEpisode)
        {
            foreach (EpisodeMetadata episodeMetadata in episode.EpisodeMetadata)
            {
                var subtitlesToExtract = new List<SubtitleToExtract>();

                // find each subtitle that needs extraction
                IEnumerable<Subtitle> subtitles = episodeMetadata.Subtitles
                    .Filter(
                        s => s.SubtitleKind == SubtitleKind.Embedded && s.IsExtracted == false &&
                             s.Codec != "hdmv_pgs_subtitle" && s.Codec != "dvd_subtitle");

                // find cache paths for each subtitle
                foreach (Subtitle subtitle in subtitles)
                {
                    Option<string> maybePath = GetRelativeOutputPath(episode.Id, subtitle);
                    foreach (string path in maybePath)
                    {
                        subtitlesToExtract.Add(new SubtitleToExtract(subtitle, path));
                    }
                }
                
                string mediaItemPath = episode.GetHeadVersion().MediaFiles.Head().Path;

                ArgumentsBuilder args = new ArgumentsBuilder()
                    .Add("-nostdin")
                    .Add("-hide_banner")
                    .Add("-i").Add(mediaItemPath);

                foreach (SubtitleToExtract subtitle in subtitlesToExtract)
                {
                    string fullOutputPath = Path.Combine(FileSystemLayout.SubtitleCacheFolder, subtitle.OutputPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath));
                    if (_localFileSystem.FileExists(fullOutputPath))
                    {
                        File.Delete(fullOutputPath);
                    }
                    args.Add("-map").Add($"0:{subtitle.Subtitle.StreamIndex}").Add("-c").Add("copy")
                        .Add(fullOutputPath);
                }
                
                BufferedCommandResult result = await Cli.Wrap(ffmpegPath)
                    .WithArguments(args.Build())
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteBufferedAsync(cancellationToken);

                if (result.ExitCode == 0)
                {
                    foreach (SubtitleToExtract subtitle in subtitlesToExtract)
                    {
                        subtitle.Subtitle.IsExtracted = true;
                        subtitle.Subtitle.Path = subtitle.OutputPath;
                    }

                    int count = await dbContext.SaveChangesAsync(cancellationToken);
                    _logger.LogDebug("Successfully extracted {Count} subtitles", count);
                }
                else
                {
                    _logger.LogError("Failed to extract subtitles. {Error}", result.StandardError);
                }
            }
        }

        return Unit.Default;
    }

    private async Task<Unit> ExtractFonts(
        TvContext dbContext,
        int mediaItemId,
        string ffmpegPath,
        CancellationToken cancellationToken)
    {
        Option<Episode> maybeEpisode = await dbContext.Episodes
            .Include(e => e.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(e => e.MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(e => e.EpisodeMetadata)
            .ThenInclude(em => em.Subtitles)
            .SelectOneAsync(e => e.Id, e => e.Id == mediaItemId);

        foreach (Episode episode in maybeEpisode)
        {
            string mediaItemPath = episode.GetHeadVersion().MediaFiles.Head().Path;

            var arguments = $"-nostdin -hide_banner -dump_attachment:t \"\" -i \"{mediaItemPath}\" -y";

            BufferedCommandResult result = await Cli.Wrap(ffmpegPath)
                .WithWorkingDirectory(FileSystemLayout.FontsCacheFolder)
                .WithArguments(arguments)
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(cancellationToken);

            // if (result.ExitCode == 0)
            // {
            //     _logger.LogDebug("Successfully extracted attached fonts");
            // }
            // else
            // {
            //     _logger.LogError("Failed to extract attached fonts. {Error}", result.StandardError);
            // }
        }

        return Unit.Default;
    }

    private static Task<Validation<BaseError, string>> FFmpegPathMustExist(TvContext dbContext) =>
        dbContext.ConfigElements.GetValue<string>(ConfigElementKey.FFmpegPath)
            .FilterT(File.Exists)
            .Map(maybePath => maybePath.ToValidation<BaseError>("FFmpeg path does not exist on filesystem"));

    private static Option<string> GetRelativeOutputPath(int mediaItemId, Subtitle subtitle)
    {
        string name = GetStringHash($"{mediaItemId}_{subtitle.StreamIndex}_{subtitle.Codec}");
        string subfolder = name[..2];
        string subfolder2 = name[2..4];

        string nameWithExtension = subtitle.Codec switch
        {
            "subrip" => $"{name}.srt",
            "ass" => $"{name}.ass",
            "webvtt" => $"{name}.vtt",
            _ => string.Empty
        };
        
        if (string.IsNullOrWhiteSpace(nameWithExtension))
        {
            return None;
        }

        return Path.Combine(subfolder, subfolder2, nameWithExtension);
    }
    
    private static string GetStringHash(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        using var md5 = MD5.Create();
        byte[] textData = Encoding.UTF8.GetBytes(text);
        byte[] hash = md5.ComputeHash(textData);
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }

    private record SubtitleToExtract(Subtitle Subtitle, string OutputPath);

    private record FontToExtract(MediaStream Stream, string OutputPath);
}
