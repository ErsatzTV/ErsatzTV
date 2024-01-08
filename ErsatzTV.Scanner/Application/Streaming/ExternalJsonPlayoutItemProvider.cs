using System.Globalization;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Streaming;
using ErsatzTV.Core.Streaming;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using ErsatzTV.Scanner.Core.Interfaces.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ErsatzTV.Scanner.Application.Streaming;

public class ExternalJsonPlayoutItemProvider : IExternalJsonPlayoutItemProvider
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly IPlexPathReplacementService _plexPathReplacementService;
    private readonly ILocalStatisticsProvider _localStatisticsProvider;
    private readonly ILogger<ExternalJsonPlayoutItemProvider> _logger;

    public ExternalJsonPlayoutItemProvider(
        IDbContextFactory<TvContext> dbContextFactory,
        ILocalFileSystem localFileSystem,
        IPlexPathReplacementService plexPathReplacementService,
        ILocalStatisticsProvider localStatisticsProvider,
        ILogger<ExternalJsonPlayoutItemProvider> logger)
    {
        _dbContextFactory = dbContextFactory;
        _localFileSystem = localFileSystem;
        _plexPathReplacementService = plexPathReplacementService;
        _localStatisticsProvider = localStatisticsProvider;
        _logger = logger;
    }

    public async Task<Either<BaseError, PlayoutItemWithPath>> CheckForExternalJson(
        Channel channel,
        DateTimeOffset now,
        string ffmpegPath,
        string ffprobePath)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        Option<Playout> maybePlayout = await dbContext.Playouts
            .AsNoTracking()
            .SelectOneAsync(p => p.ChannelId, p => p.ChannelId == channel.Id);

        foreach (Playout playout in maybePlayout)
        {
            // playout must be external json
            if (playout.ProgramSchedulePlayoutType == ProgramSchedulePlayoutType.ExternalJson)
            {
                // json file must exist
                if (_localFileSystem.FileExists(playout.ExternalJsonFile))
                {
                    return await GetExternalJsonPlayoutItem(dbContext, playout, now, ffmpegPath, ffprobePath);
                }

                _logger.LogWarning(
                    "Unable to locate external json file {File} for channel {Number} - {Name}",
                    playout.ExternalJsonFile,
                    channel.Number,
                    channel.Name);
            }
        }

        return new UnableToLocatePlayoutItem();
    }
    
    private async Task<Either<BaseError, PlayoutItemWithPath>> GetExternalJsonPlayoutItem(
        TvContext dbContext,
        Playout playout,
        DateTimeOffset now,
        string ffmpegPath,
        string ffprobePath)
    {
        Option<ExternalJsonChannel> maybeChannel = JsonConvert.DeserializeObject<ExternalJsonChannel>(
            await File.ReadAllTextAsync(playout.ExternalJsonFile));

        // must deserialize channel from json
        foreach (ExternalJsonChannel channel in maybeChannel)
        {
            DateTimeOffset startTime = DateTimeOffset.Parse(
                channel.StartTime ?? string.Empty,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal).ToLocalTime();

            //_logger.LogDebug("external json start time: {StartTime}", startTime);

            foreach (ExternalJsonProgram program in channel.Programs)
            {
                int milliseconds = program.Duration;
                DateTimeOffset nextStart = startTime + TimeSpan.FromMilliseconds(milliseconds);
                if (nextStart > now)
                {
                    //_logger.LogDebug("should play program {@Program}", program);
                    return await BuildPlayoutItem(dbContext, startTime, program, ffmpegPath, ffprobePath);
                }

                startTime = nextStart;
            }
        }

        return new UnableToLocatePlayoutItem();
    }

    private async Task<Either<BaseError, PlayoutItemWithPath>> BuildPlayoutItem(
        TvContext dbContext,
        DateTimeOffset startTime,
        ExternalJsonProgram program,
        string ffmpegPath,
        string ffprobePath)
    {
        // find any library path from the appropriate plex server
        List<LibraryPath> maybeLibraryPath = await dbContext.LibraryPaths
            .Filter(lp => ((PlexMediaSource)((PlexLibrary)lp.Library).MediaSource).ServerName == program.ServerKey)
            .Take(1)
            .ToListAsync();

        foreach (LibraryPath libraryPath in maybeLibraryPath.HeadOrNone())
        {
            string localPath = await _plexPathReplacementService.GetReplacementPlexPath(
                libraryPath.Id,
                program.File);

            if (_localFileSystem.FileExists(localPath))
            {
                return await StreamLocally(startTime, program, ffmpegPath, ffprobePath, localPath);
            }

            return await StreamRemotely(startTime, program);
        }
        
        return new UnableToLocatePlayoutItem();
    }

    private async Task<Either<BaseError, PlayoutItemWithPath>> StreamLocally(
        DateTimeOffset startTime,
        ExternalJsonProgram program,
        string ffmpegPath,
        string ffprobePath,
        string localPath)
    {
        // ffprobe on demand
        Either<BaseError, MediaVersion> maybeMediaVersion =
            await _localStatisticsProvider.GetStatistics(ffmpegPath, ffprobePath, localPath);

        foreach (MediaVersion mediaVersion in maybeMediaVersion.RightToSeq())
        {
            // build playout item
            return new PlayoutItemWithPath(
                new PlayoutItem
                {
                    Start = startTime.UtcDateTime,
                    Finish = startTime.AddMilliseconds(program.Duration).UtcDateTime,
                    FillerKind = FillerKind.None,
                    ChapterTitle = null,
                    GuideFinish = null,
                    GuideGroup = 0,
                    CustomTitle = null,
                    InPoint = TimeSpan.Zero,
                    OutPoint = TimeSpan.FromMilliseconds(program.Duration),
                    // TODO: other video/filler?
                    MediaItem = new Episode
                    {
                        MediaVersions = [mediaVersion],
                        EpisodeMetadata =
                        [
                            new EpisodeMetadata
                            {
                                EpisodeNumber = program.Episode,
                                Title = program.Title,
                            },
                        ],
                        Season = new Season
                        {
                            SeasonNumber = program.Season,
                            Show = new Show
                            {
                                ShowMetadata =
                                [
                                    new ShowMetadata
                                    {
                                        Title = program.ShowTitle
                                    }
                                ]
                            }
                        }
                    }
                },
                localPath);
        }

        return new UnableToLocatePlayoutItem();
    }
    
    private static async Task<Either<BaseError, PlayoutItemWithPath>> StreamRemotely(
        DateTimeOffset startTime,
        ExternalJsonProgram program)
    {
        await Task.Delay(10);
        
        throw new NotImplementedException();
    }
}
