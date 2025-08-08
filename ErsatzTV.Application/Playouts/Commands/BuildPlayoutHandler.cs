using System.Threading.Channels;
using Bugsnag;
using EFCore.BulkExtensions;
using ErsatzTV.Application.Channels;
using ErsatzTV.Application.Subtitles;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts;

public class BuildPlayoutHandler : IRequestHandler<BuildPlayout, Either<BaseError, Unit>>
{
    private readonly IBlockPlayoutBuilder _blockPlayoutBuilder;
    private readonly IBlockPlayoutFillerBuilder _blockPlayoutFillerBuilder;
    private readonly IClient _client;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IEntityLocker _entityLocker;
    private readonly IExternalJsonPlayoutBuilder _externalJsonPlayoutBuilder;
    private readonly IFFmpegSegmenterService _ffmpegSegmenterService;
    private readonly IPlayoutBuilder _playoutBuilder;
    private readonly ChannelWriter<IBackgroundServiceRequest> _workerChannel;
    private readonly IYamlPlayoutBuilder _yamlPlayoutBuilder;

    public BuildPlayoutHandler(
        IClient client,
        IDbContextFactory<TvContext> dbContextFactory,
        IPlayoutBuilder playoutBuilder,
        IBlockPlayoutBuilder blockPlayoutBuilder,
        IBlockPlayoutFillerBuilder blockPlayoutFillerBuilder,
        IYamlPlayoutBuilder yamlPlayoutBuilder,
        IExternalJsonPlayoutBuilder externalJsonPlayoutBuilder,
        IFFmpegSegmenterService ffmpegSegmenterService,
        IEntityLocker entityLocker,
        ChannelWriter<IBackgroundServiceRequest> workerChannel)
    {
        _client = client;
        _dbContextFactory = dbContextFactory;
        _playoutBuilder = playoutBuilder;
        _blockPlayoutBuilder = blockPlayoutBuilder;
        _blockPlayoutFillerBuilder = blockPlayoutFillerBuilder;
        _yamlPlayoutBuilder = yamlPlayoutBuilder;
        _externalJsonPlayoutBuilder = externalJsonPlayoutBuilder;
        _ffmpegSegmenterService = ffmpegSegmenterService;
        _entityLocker = entityLocker;
        _workerChannel = workerChannel;
    }

    public async Task<Either<BaseError, Unit>> Handle(BuildPlayout request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Playout> validation = await Validate(dbContext, request);
        return await validation.Match(
            playout => ApplyUpdateRequest(dbContext, request, playout, cancellationToken),
            error => Task.FromResult<Either<BaseError, Unit>>(error.Join()));
    }

    private async Task<Either<BaseError, Unit>> ApplyUpdateRequest(
        TvContext dbContext,
        BuildPlayout request,
        Playout playout,
        CancellationToken cancellationToken)
    {
        string channelNumber;
        string channelName = "[unknown]";

        try
        {
            await _entityLocker.LockPlayout(playout.Id);

            var referenceData = await GetReferenceData(dbContext, playout.Id, playout.ProgramSchedulePlayoutType);
            channelNumber = referenceData.Channel.Number;
            channelName = referenceData.Channel.Name;
            var result = PlayoutBuildResult.Empty;

            switch (playout.ProgramSchedulePlayoutType)
            {
                case ProgramSchedulePlayoutType.Block:
                    result = await _blockPlayoutBuilder.Build(playout, referenceData, request.Mode, cancellationToken);
                    result = await _blockPlayoutFillerBuilder.Build(playout, referenceData, result, request.Mode, cancellationToken);
                    break;
                case ProgramSchedulePlayoutType.Yaml:
                    result = await _yamlPlayoutBuilder.Build(playout, referenceData, request.Mode, cancellationToken);
                    break;
                case ProgramSchedulePlayoutType.ExternalJson:
                    await _externalJsonPlayoutBuilder.Build(playout, request.Mode, cancellationToken);
                    break;
                case ProgramSchedulePlayoutType.None:
                case ProgramSchedulePlayoutType.Classic:
                default:
                    result = await _playoutBuilder.Build(playout, referenceData, request.Mode, cancellationToken);
                    break;
            }

            int changeCount = 0;

            if (result.ClearItems)
            {
                changeCount += await dbContext.PlayoutItems
                    .Where(pi => pi.PlayoutId == playout.Id)
                    .ExecuteDeleteAsync(cancellationToken);
            }

            foreach (var removeBefore in result.RemoveBefore)
            {
                changeCount += await dbContext.PlayoutItems
                    .Where(pi => pi.PlayoutId == playout.Id)
                    .Where(pi => pi.Finish < removeBefore.UtcDateTime)
                    .ExecuteDeleteAsync(cancellationToken);
            }

            foreach (var removeAfter in result.RemoveAfter)
            {
                changeCount += await dbContext.PlayoutItems
                    .Where(pi => pi.PlayoutId == playout.Id)
                    .Where(pi => pi.Start >= removeAfter.UtcDateTime)
                    .ExecuteDeleteAsync(cancellationToken);
            }

            if (result.ItemsToRemove.Count > 0)
            {
                changeCount += await dbContext.PlayoutItems
                    .Where(pi => result.ItemsToRemove.Contains(pi.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            if (result.AddedItems.Count > 0)
            {
                changeCount += 1;
                var bulkConfig = new BulkConfig();
                bool anyWatermarks = result.AddedItems.Any(i => i.PlayoutItemWatermarks is not null && i.PlayoutItemWatermarks.Count > 0);
                bool anyGraphicsElements = result.AddedItems.Any(i => i.PlayoutItemGraphicsElements is not null && i.PlayoutItemGraphicsElements.Count > 0);
                if (anyWatermarks || anyGraphicsElements)
                {
                    bulkConfig.SetOutputIdentity = true;
                }

                await dbContext.BulkInsertAsync(result.AddedItems, bulkConfig, cancellationToken: cancellationToken);

                if (anyWatermarks)
                {
                    // copy playout item ids back to watermarks
                    var allWatermarks = result.AddedItems.SelectMany(item =>
                        item.PlayoutItemWatermarks.Select(watermark =>
                        {
                            watermark.PlayoutItemId = item.Id;
                            watermark.PlayoutItem = null;
                            return watermark;
                        })
                    ).ToList();

                    await dbContext.BulkInsertAsync(allWatermarks, cancellationToken: cancellationToken);
                }

                if (anyGraphicsElements)
                {
                    // copy playout item ids back to graphics elements
                    var allGraphicsElements = result.AddedItems.SelectMany(item =>
                        item.PlayoutItemGraphicsElements.Select(graphicsElement =>
                        {
                            graphicsElement.PlayoutItemId = item.Id;
                            graphicsElement.PlayoutItem = null;
                            return graphicsElement;
                        })
                    ).ToList();

                    await dbContext.BulkInsertAsync(allGraphicsElements, cancellationToken: cancellationToken);
                }
            }

            if (result.HistoryToRemove.Count > 0)
            {
                changeCount += await dbContext.PlayoutHistory
                    .Where(ph => result.HistoryToRemove.Contains(ph.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            if (result.AddedHistory.Count > 0)
            {
                changeCount += 1;
                await dbContext.BulkInsertAsync(result.AddedHistory, cancellationToken: cancellationToken);
            }

            // let any active segmenter processes know that the playout has been modified
            // and therefore the segmenter may need to seek into the next item instead of
            // starting at the beginning (if already working ahead)
            changeCount += await dbContext.SaveChangesAsync(cancellationToken);
            bool hasChanges = changeCount > 0;

            if (request.Mode != PlayoutBuildMode.Continue && hasChanges)
            {
                _ffmpegSegmenterService.PlayoutUpdated(referenceData.Channel.Number);
            }

            string fileName = Path.Combine(FileSystemLayout.ChannelGuideCacheFolder, $"{channelNumber}.xml");
            if (hasChanges || !File.Exists(fileName) ||
                playout.ProgramSchedulePlayoutType is ProgramSchedulePlayoutType.ExternalJson)
            {
                await _workerChannel.WriteAsync(new RefreshChannelData(channelNumber), cancellationToken);
            }

            await _workerChannel.WriteAsync(new ExtractEmbeddedSubtitles(playout.Id), cancellationToken);
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            _client.Notify(ex);
            return BaseError.New(
                $"Timeout building playout for channel {channelName}; this may be a bug!");
        }
        catch (Exception ex)
        {
            DebugBreak.Break();

            _client.Notify(ex);
            return BaseError.New(
                $"Unexpected error building playout for channel {channelName}: {ex.Message}");
        }
        finally
        {
            await _entityLocker.UnlockPlayout(playout.Id);
        }

        return Unit.Default;
    }

    private static Task<Validation<BaseError, Playout>> Validate(TvContext dbContext, BuildPlayout request) =>
        PlayoutMustExist(dbContext, request).BindT(DiscardAttemptsMustBeValid);

    private static Validation<BaseError, Playout> DiscardAttemptsMustBeValid(Playout playout)
    {
        foreach (ProgramScheduleItemDuration item in
                 playout.ProgramSchedule?.Items.OfType<ProgramScheduleItemDuration>() ?? [])
        {
            item.DiscardToFillAttempts = item.PlaybackOrder switch
            {
                PlaybackOrder.Random or PlaybackOrder.Shuffle => item.DiscardToFillAttempts,
                _ => 0
            };
        }

        return playout;
    }

    private static async Task<Validation<BaseError, Playout>> PlayoutMustExist(
        TvContext dbContext,
        BuildPlayout buildPlayout)
    {
        var maybePlayout = await dbContext.Playouts
            .SelectOneAsync(p => p.Id, p => p.Id == buildPlayout.PlayoutId);

        foreach (var playout in maybePlayout)
        {
            switch (playout.ProgramSchedulePlayoutType)
            {
                case ProgramSchedulePlayoutType.Classic:
                    await dbContext.Entry(playout)
                        .Collection(p => p.FillGroupIndices)
                        .LoadAsync();

                    foreach (var fillGroupIndex in playout.FillGroupIndices)
                    {
                        await dbContext.Entry(fillGroupIndex)
                            .Reference(fgi => fgi.EnumeratorState)
                            .LoadAsync();
                    }

                    await dbContext.Entry(playout)
                        .Collection(p => p.ProgramScheduleAnchors)
                        .LoadAsync();

                    foreach (var anchor in playout.ProgramScheduleAnchors)
                    {
                        await dbContext.Entry(anchor)
                            .Reference(a => a.EnumeratorState)
                            .LoadAsync();
                    }

                    break;
            }
        }

        return maybePlayout.ToValidation<BaseError>("Playout does not exist.");
    }

    private static async Task<PlayoutReferenceData> GetReferenceData(
        TvContext dbContext,
        int playoutId,
        ProgramSchedulePlayoutType playoutType)
    {
        var channel = await dbContext.Channels
            .AsNoTracking()
            .Where(c => c.Playouts.Any(p => p.Id == playoutId))
            .FirstOrDefaultAsync();

        var deco = Option<Deco>.None;
        List<PlayoutItem> existingItems = [];
        List<PlayoutTemplate> playoutTemplates = [];

        if (playoutType is ProgramSchedulePlayoutType.Block)
        {
            deco = await dbContext.Decos
                .AsNoTracking()
                .Where(d => d.Playouts.Any(p => p.Id == playoutId))
                .FirstOrDefaultAsync()
                .Map(Optional);

            existingItems = await dbContext.PlayoutItems
                .AsNoTracking()
                .Where(pi => pi.PlayoutId == playoutId)
                .ToListAsync();

            playoutTemplates = await dbContext.PlayoutTemplates
                .AsNoTracking()
                .Where(pt => pt.PlayoutId == playoutId)
                .Include(t => t.Template)
                .ThenInclude(t => t.Items)
                .ThenInclude(i => i.Block)
                .ThenInclude(b => b.Items)
                .Include(t => t.DecoTemplate)
                .ThenInclude(t => t.Items)
                .ThenInclude(i => i.Deco)
                .ToListAsync();
        }

        var programSchedule = await dbContext.ProgramSchedules
            .AsNoTracking()
            .Where(ps => ps.Playouts.Any(p => p.Id == playoutId))
            .Include(ps => ps.Items)
            .ThenInclude(psi => psi.Watermark)
            .Include(ps => ps.Items)
            .ThenInclude(psi => psi.Collection)
            .Include(ps => ps.Items)
            .ThenInclude(psi => psi.MediaItem)
            .Include(ps => ps.Items)
            .ThenInclude(psi => psi.PreRollFiller)
            .Include(ps => ps.Items)
            .ThenInclude(psi => psi.MidRollFiller)
            .Include(ps => ps.Items)
            .ThenInclude(psi => psi.PostRollFiller)
            .Include(ps => ps.Items)
            .ThenInclude(psi => psi.TailFiller)
            .Include(ps => ps.Items)
            .ThenInclude(psi => psi.FallbackFiller)
            .FirstOrDefaultAsync();

        var programScheduleAlternates = await dbContext.ProgramScheduleAlternates
            .AsNoTracking()
            .Where(pt => pt.PlayoutId == playoutId)
            .Include(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.Watermark)
            .Include(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.Collection)
            .Include(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.MediaItem)
            .Include(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.PreRollFiller)
            .Include(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.MidRollFiller)
            .Include(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.PostRollFiller)
            .Include(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.TailFiller)
            .Include(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.FallbackFiller)
            .ToListAsync();

        var playoutHistory = await dbContext.PlayoutHistory
            .AsNoTracking()
            .Where(h => h.PlayoutId == playoutId)
            .ToListAsync();

        return new PlayoutReferenceData(
            channel,
            deco,
            existingItems,
            playoutTemplates,
            programSchedule,
            programScheduleAlternates,
            playoutHistory);
    }
}
