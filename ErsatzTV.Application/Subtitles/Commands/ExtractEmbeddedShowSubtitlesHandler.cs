using System.Threading.Channels;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Dapper;
using ErsatzTV.Application.Maintenance;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.Subtitles;

public class ExtractEmbeddedShowSubtitlesHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    ChannelWriter<IBackgroundServiceRequest> workerChannel,
    IConfigElementRepository configElementRepository,
    ILocalFileSystem localFileSystem,
    ILogger<ExtractEmbeddedSubtitlesHandler> logger)
    : ExtractEmbeddedSubtitlesHandlerBase(localFileSystem, logger),
        IRequestHandler<ExtractEmbeddedShowSubtitles, Option<BaseError>>
{
    public async Task<Option<BaseError>> Handle(
        ExtractEmbeddedShowSubtitles request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, string> validation = await FFmpegPathMustExist(dbContext, cancellationToken);
        return await validation.Match(
            async ffmpegPath =>
            {
                Option<BaseError> result = await ExtractAll(dbContext, request, ffmpegPath, cancellationToken);
                await workerChannel.WriteAsync(new ReleaseMemory(false), cancellationToken);
                return result;
            },
            error => Task.FromResult<Option<BaseError>>(error.Join()));
    }

    private async Task<Option<BaseError>> ExtractAll(
        TvContext dbContext,
        ExtractEmbeddedShowSubtitles request,
        string ffmpegPath,
        CancellationToken cancellationToken)
    {
        try
        {
            bool useEmbeddedSubtitles = await configElementRepository
                .GetValue<bool>(ConfigElementKey.FFmpegUseEmbeddedSubtitles, cancellationToken)
                .IfNoneAsync(true);

            if (!useEmbeddedSubtitles)
            {
                return Option<BaseError>.None;
            }

            bool extractEmbeddedSubtitles = await configElementRepository
                .GetValue<bool>(ConfigElementKey.FFmpegExtractEmbeddedSubtitles, cancellationToken)
                .IfNoneAsync(false);

            if (!extractEmbeddedSubtitles)
            {
                return Option<BaseError>.None;
            }

            Option<Show> maybeShow = await dbContext.Shows
                .AsNoTracking()
                .Include(s => s.ShowMetadata)
                .SelectOneAsync(s => s.Id, s => s.Id == request.ShowId, cancellationToken);
            foreach (ShowMetadata showMetadata in maybeShow.SelectMany(s => s.ShowMetadata).HeadOrNone())
            {
                logger.LogDebug("Checking show {ShowTitle} for text subtitles to extract", showMetadata.Title);

                // check for episodes with not-extracted subtitles
                List<int> episodeIds = await dbContext.Connection.QueryAsync<int>(
                        """
                        SELECT DISTINCT E.Id
                        FROM Episode E
                        INNER JOIN Season S ON S.Id = E.SeasonId
                        INNER JOIN EpisodeMetadata EM ON EM.EpisodeId = E.Id
                        INNER JOIN Subtitle SUB ON SUB.EpisodeMetadataId = EM.Id
                        WHERE S.ShowId = @ShowId AND SUB.SubtitleKind=0 AND (SUB.IsExtracted=0 OR SUB.Path IS NULL)
                        """,
                        new { request.ShowId })
                    .Map(result => result.ToList());

                // filter for items with text subtitles or font attachments
                List<int> episodeIdsWithTextSubtitles =
                    await GetMediaItemIdsWithTextSubtitles(dbContext, episodeIds, cancellationToken);

                logger.LogDebug(
                    "Show {ShowTitle} has {EpisodeCount} episodes with text subtitles to extract",
                    showMetadata.Title,
                    episodeIdsWithTextSubtitles.Count);

                for (var i = 0; i < episodeIdsWithTextSubtitles.Count; i++)
                {
                    logger.LogDebug(
                        "Extracting text subtitles for show {ShowTitle} - {Current} of {Total}",
                        showMetadata.Title,
                        i + 1,
                        episodeIdsWithTextSubtitles.Count);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return Option<BaseError>.None;
                    }

                    // extract subtitles and fonts for each item and update db
                    int mediaItemId = episodeIdsWithTextSubtitles[i];
                    await ExtractSubtitles(dbContext, mediaItemId, ffmpegPath, cancellationToken);
                    await ExtractFonts(dbContext, mediaItemId, ffmpegPath, cancellationToken);
                }

                logger.LogDebug("Done checking show {ShowTitle} for text subtitles to extract", showMetadata.Title);
            }
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            // do nothing
        }

        return Option<BaseError>.None;
    }
}
