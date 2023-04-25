using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using CliWrap;
using CliWrap.Buffered;
using CliWrap.Builders;
using Dapper;
using ErsatzTV.Application.Maintenance;
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
    private readonly ChannelWriter<IBackgroundServiceRequest> _workerChannel;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger<ExtractEmbeddedSubtitlesHandler> _logger;

    public ExtractEmbeddedSubtitlesHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        ILocalFileSystem localFileSystem,
        ChannelWriter<IBackgroundServiceRequest> workerChannel,
        ILogger<ExtractEmbeddedSubtitlesHandler> logger)
    {
        _dbContextFactory = dbContextFactory;
        _localFileSystem = localFileSystem;
        _workerChannel = workerChannel;
        _logger = logger;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        ExtractEmbeddedSubtitles request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, string> validation = await FFmpegPathMustExist(dbContext);
        return await validation.Match(
            async ffmpegPath =>
            {
                Either<BaseError, Unit> result = await ExtractAll(dbContext, request, ffmpegPath, cancellationToken);
                await _workerChannel.WriteAsync(new ReleaseMemory(false), cancellationToken);
                return result;
            },
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

            var playoutIdsToCheck = new List<int>();

            // only check the requested playout if subtitles are enabled
            Option<Playout> requestedPlayout = await dbContext.Playouts
                .AsNoTracking()
                .Filter(
                    p => p.Channel.SubtitleMode != ChannelSubtitleMode.None ||
                         p.ProgramSchedule.Items.Any(psi => psi.SubtitleMode != ChannelSubtitleMode.None))
                .SelectOneAsync(p => p.Id, p => p.Id == request.PlayoutId.IfNone(-1));

            playoutIdsToCheck.AddRange(requestedPlayout.Map(p => p.Id));

            // check all playouts (that have subtitles enabled) if none were passed
            if (request.PlayoutId.IsNone)
            {
                playoutIdsToCheck = dbContext.Playouts
                    .AsNoTracking()
                    .Filter(
                        p => p.Channel.SubtitleMode != ChannelSubtitleMode.None ||
                             p.ProgramSchedule.Items.Any(psi => psi.SubtitleMode != ChannelSubtitleMode.None))
                    .Map(p => p.Id)
                    .ToList();
            }

            if (playoutIdsToCheck.Count == 0)
            {
                foreach (int playoutId in request.PlayoutId)
                {
                    _logger.LogDebug(
                        "Playout {PlayoutId} does not have subtitles enabled; nothing to extract",
                        playoutId);
                    return Unit.Default;
                }

                _logger.LogDebug("No playouts have subtitles enabled; nothing to extract");
                return Unit.Default;
            }

            _logger.LogDebug("Checking playouts {PlayoutIds} for text subtitles to extract", playoutIdsToCheck);

            // find all playout items in the next hour
            List<PlayoutItem> playoutItems = await dbContext.PlayoutItems
                .AsNoTracking()
                .Filter(pi => playoutIdsToCheck.Contains(pi.PlayoutId))
                .Filter(pi => pi.Finish >= DateTime.UtcNow)
                .Filter(pi => pi.Start <= until)
                .ToListAsync(cancellationToken);

            var mediaItemIds = playoutItems.Map(pi => pi.MediaItemId).ToList();

            // filter for items with text subtitles or font attachments
            List<int> mediaItemIdsWithTextSubtitles =
                await GetMediaItemIdsWithTextSubtitles(dbContext, mediaItemIds, cancellationToken);

            if (mediaItemIdsWithTextSubtitles.Any())
            {
                _logger.LogDebug(
                    "Checking media items {MediaItemIds} for text subtitles or fonts to extract for playouts {PlayoutIds}",
                    mediaItemIdsWithTextSubtitles,
                    playoutIdsToCheck);
            }
            else
            {
                _logger.LogDebug(
                    "Found no text subtitles or fonts to extract for playouts {PlayoutIds}",
                    playoutIdsToCheck);
            }

            // sort by start time
            var toUpdate = playoutItems
                .Filter(pi => pi.Finish >= DateTime.UtcNow)
                .DistinctBy(pi => pi.MediaItemId)
                .Filter(pi => mediaItemIdsWithTextSubtitles.Contains(pi.MediaItemId))
                .OrderBy(pi => pi.StartOffset)
                .Map(pi => pi.MediaItemId)
                .ToList();

            foreach (int mediaItemId in toUpdate)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return Unit.Default;
                }

                // extract subtitles and fonts for each item and update db
                await ExtractSubtitles(dbContext, mediaItemId, ffmpegPath, cancellationToken);
                await ExtractFonts(dbContext, mediaItemId, ffmpegPath, cancellationToken);
            }

            _logger.LogDebug("Done checking playouts {PlayoutIds} for text subtitles to extract", playoutIdsToCheck);

            return Unit.Default;
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            return Unit.Default;
        }
    }

    private async Task<List<int>> GetMediaItemIdsWithTextSubtitles(
        TvContext dbContext,
        List<int> mediaItemIds,
        CancellationToken cancellationToken)
    {
        var result = new List<int>();

        try
        {
            List<int> episodeIds = await dbContext.EpisodeMetadata
                .AsNoTracking()
                .Filter(em => mediaItemIds.Contains(em.EpisodeId))
                .Filter(
                    em => em.Subtitles.Any(
                        s => s.SubtitleKind == SubtitleKind.Embedded &&
                             s.Codec != "hdmv_pgs_subtitle" && s.Codec != "dvd_subtitle" && s.Codec != "dvdsub" && s.Codec != "vobsub" && s.Codec != "pgssub"))
                .Map(em => em.EpisodeId)
                .ToListAsync(cancellationToken);
            result.AddRange(episodeIds);

            List<int> movieIds = await dbContext.MovieMetadata
                .AsNoTracking()
                .Filter(mm => mediaItemIds.Contains(mm.MovieId))
                .Filter(
                    mm => mm.Subtitles.Any(
                        s => s.SubtitleKind == SubtitleKind.Embedded &&
                             s.Codec != "hdmv_pgs_subtitle" && s.Codec != "dvd_subtitle" && s.Codec != "dvdsub" && s.Codec != "vobsub" && s.Codec != "pgssub"))
                .Map(mm => mm.MovieId)
                .ToListAsync(cancellationToken);
            result.AddRange(movieIds);

            List<int> musicVideoIds = await dbContext.MusicVideoMetadata
                .AsNoTracking()
                .Filter(mm => mediaItemIds.Contains(mm.MusicVideoId))
                .Filter(
                    mm => mm.Subtitles.Any(
                        s => s.SubtitleKind == SubtitleKind.Embedded &&
                             s.Codec != "hdmv_pgs_subtitle" && s.Codec != "dvd_subtitle" && s.Codec != "dvdsub" && s.Codec != "vobsub" && s.Codec != "pgssub"))
                .Map(mm => mm.MusicVideoId)
                .ToListAsync(cancellationToken);
            result.AddRange(musicVideoIds);

            List<int> otherVideoIds = await dbContext.OtherVideoMetadata
                .AsNoTracking()
                .Filter(ovm => mediaItemIds.Contains(ovm.OtherVideoId))
                .Filter(
                    ovm => ovm.Subtitles.Any(
                        s => s.SubtitleKind == SubtitleKind.Embedded &&
                             s.Codec != "hdmv_pgs_subtitle" && s.Codec != "dvd_subtitle" && s.Codec != "dvdsub" && s.Codec != "vobsub" && s.Codec != "pgssub"))
                .Map(ovm => ovm.OtherVideoId)
                .ToListAsync(cancellationToken);
            result.AddRange(otherVideoIds);
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            // do nothing
        }

        return result;
    }

    private async Task ExtractSubtitles(
        TvContext dbContext,
        int mediaItemId,
        string ffmpegPath,
        CancellationToken cancellationToken)
    {
        foreach (MediaItem mediaItem in await GetMediaItem(dbContext, mediaItemId))
        {
            foreach (List<Subtitle> allSubtitles in GetSubtitles(mediaItem))
            {
                var subtitlesToExtract = new List<SubtitleToExtract>();

                // find each subtitle that needs extraction
                IEnumerable<Subtitle> subtitles = allSubtitles
                    .Filter(
                        s => s.SubtitleKind == SubtitleKind.Embedded && s.IsExtracted == false &&
                             s.Codec != "hdmv_pgs_subtitle" && s.Codec != "dvd_subtitle" && s.Codec != "dvdsub" && s.Codec != "vobsub" && s.Codec != "pgssub");

                // find cache paths for each subtitle
                foreach (Subtitle subtitle in subtitles)
                {
                    Option<string> maybePath = GetRelativeOutputPath(mediaItem.Id, subtitle);
                    foreach (string path in maybePath)
                    {
                        subtitlesToExtract.Add(new SubtitleToExtract(subtitle, path));
                    }
                }

                if (subtitlesToExtract.Count == 0)
                {
                    continue;
                }

                string mediaItemPath = await GetMediaItemPath(dbContext, mediaItem);

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

                    args.Add("-map").Add($"0:{subtitle.Subtitle.StreamIndex}")
                        .Add("-c:s").Add(subtitle.Subtitle.Codec == "mov_text" ? "text" : "copy")
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
    }

    private static async Task<Option<MediaItem>> GetMediaItem(TvContext dbContext, int mediaItemId) =>
        await dbContext.MediaItems
            .Include(mi => (mi as Episode).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(mi => (mi as Episode).MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(mi => (mi as Episode).EpisodeMetadata)
            .ThenInclude(em => em.Subtitles)
            .Include(mi => (mi as Movie).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(mi => (mi as Movie).MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(mi => (mi as Movie).MovieMetadata)
            .ThenInclude(em => em.Subtitles)
            .Include(mi => (mi as MusicVideo).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(mi => (mi as MusicVideo).MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(mi => (mi as MusicVideo).MusicVideoMetadata)
            .ThenInclude(em => em.Subtitles)
            .Include(mi => (mi as OtherVideo).MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(mi => (mi as OtherVideo).MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(mi => (mi as OtherVideo).OtherVideoMetadata)
            .ThenInclude(em => em.Subtitles)
            .SelectOneAsync(e => e.Id, e => e.Id == mediaItemId);

    private static Option<List<Subtitle>> GetSubtitles(MediaItem mediaItem) =>
        mediaItem switch
        {
            Episode e => e.EpisodeMetadata.Head().Subtitles,
            Movie m => m.MovieMetadata.Head().Subtitles,
            MusicVideo mv => mv.MusicVideoMetadata.Head().Subtitles,
            OtherVideo ov => ov.OtherVideoMetadata.Head().Subtitles,
            _ => None
        };

    private async Task ExtractFonts(
        TvContext dbContext,
        int mediaItemId,
        string ffmpegPath,
        CancellationToken cancellationToken)
    {
        foreach (MediaItem mediaItem in await GetMediaItem(dbContext, mediaItemId))
        {
            MediaVersion headVersion = mediaItem.GetHeadVersion();
            var attachments = headVersion.Streams
                .Filter(s => s.MediaStreamKind == MediaStreamKind.Attachment)
                .OrderBy(s => s.Index)
                .ToList();

            for (var attachmentIndex = 0; attachmentIndex < attachments.Count; attachmentIndex++)
            {
                MediaStream fontStream = attachments[attachmentIndex];

                if (!(fontStream.MimeType ?? string.Empty).Contains("font") &&
                    !(fontStream.MimeType ?? string.Empty).Contains("opentype"))
                {
                    // not a font
                    continue;
                }

                string fullOutputPath = Path.Combine(FileSystemLayout.FontsCacheFolder, fontStream.FileName);
                if (_localFileSystem.FileExists(fullOutputPath))
                {
                    // already extracted
                    continue;
                }

                string mediaItemPath = await GetMediaItemPath(dbContext, mediaItem);

                var arguments =
                    $"-nostdin -hide_banner -dump_attachment:t:{attachmentIndex} \"\" -i \"{mediaItemPath}\" -y";

                BufferedCommandResult result = await Cli.Wrap(ffmpegPath)
                    .WithWorkingDirectory(FileSystemLayout.FontsCacheFolder)
                    .WithArguments(arguments)
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteBufferedAsync(cancellationToken);

                // ffmpeg seems to return exit code 1 in all cases when dumping an attachment
                // so ignore it and check success a different way
                if (_localFileSystem.FileExists(fullOutputPath))
                {
                    _logger.LogDebug("Successfully extracted font {Font}", fontStream.FileName);
                }
                else
                {
                    _logger.LogError(
                        "Failed to extract attached font {Font}. {Error}",
                        fontStream.FileName,
                        result.StandardError);
                }
            }
        }
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
            "subrip" or "srt" => $"{name}.srt",
            "ass" => $"{name}.ass",
            "webvtt" => $"{name}.vtt",
            "mov_text" => $"{name}.srt",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(nameWithExtension))
        {
            return None;
        }

        return Path.Combine(subfolder, subfolder2, nameWithExtension);
    }

    private static async Task<string> GetMediaItemPath(TvContext dbContext, MediaItem mediaItem)
    {
        MediaVersion version = mediaItem.GetHeadVersion();

        MediaFile file = version.MediaFiles.Head();
        switch (file)
        {
            case PlexMediaFile pmf:
                Option<int> maybeId = await dbContext.Connection.QuerySingleOrDefaultAsync<int>(
                        @"SELECT PMS.Id FROM PlexMediaSource PMS
                  INNER JOIN Library L on PMS.Id = L.MediaSourceId
                  INNER JOIN LibraryPath LP on L.Id = LP.LibraryId
                  WHERE LP.Id = @LibraryPathId",
                        new { mediaItem.LibraryPathId })
                    .Map(Optional);

                foreach (int plexMediaSourceId in maybeId)
                {
                    return $"http://localhost:{Settings.ListenPort}/media/plex/{plexMediaSourceId}/{pmf.Key}";
                }

                break;
        }

        return mediaItem switch
        {
            JellyfinMovie jellyfinMovie =>
                $"http://localhost:{Settings.ListenPort}/media/jellyfin/{jellyfinMovie.ItemId}",
            JellyfinEpisode jellyfinEpisode =>
                $"http://localhost:{Settings.ListenPort}/media/jellyfin/{jellyfinEpisode.ItemId}",
            EmbyMovie embyMovie => $"http://localhost:{Settings.ListenPort}/media/emby/{embyMovie.ItemId}",
            EmbyEpisode embyEpisode => $"http://localhost:{Settings.ListenPort}/media/emby/{embyEpisode.ItemId}",
            _ => file.Path
        };
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
}
