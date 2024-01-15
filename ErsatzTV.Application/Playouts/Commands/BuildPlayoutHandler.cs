using System.Threading.Channels;
using Bugsnag;
using Dapper;
using ErsatzTV.Application.Channels;
using ErsatzTV.Application.Subtitles;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Playouts;

public class BuildPlayoutHandler : IRequestHandler<BuildPlayout, Either<BaseError, Unit>>
{
    private readonly IClient _client;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IEntityLocker _entityLocker;
    private readonly ILogger<BuildPlayoutHandler> _logger;
    private readonly IFFmpegSegmenterService _ffmpegSegmenterService;
    private readonly IPlayoutBuilder _playoutBuilder;
    private readonly IBlockPlayoutBuilder _blockPlayoutBuilder;
    private readonly IExternalJsonPlayoutBuilder _externalJsonPlayoutBuilder;
    private readonly ChannelWriter<IBackgroundServiceRequest> _workerChannel;

    public BuildPlayoutHandler(
        IClient client,
        IDbContextFactory<TvContext> dbContextFactory,
        IPlayoutBuilder playoutBuilder,
        IBlockPlayoutBuilder blockPlayoutBuilder,
        IExternalJsonPlayoutBuilder externalJsonPlayoutBuilder,
        IFFmpegSegmenterService ffmpegSegmenterService,
        IEntityLocker entityLocker,
        ILogger<BuildPlayoutHandler> logger,
        ChannelWriter<IBackgroundServiceRequest> workerChannel)
    {
        _client = client;
        _dbContextFactory = dbContextFactory;
        _playoutBuilder = playoutBuilder;
        _blockPlayoutBuilder = blockPlayoutBuilder;
        _externalJsonPlayoutBuilder = externalJsonPlayoutBuilder;
        _ffmpegSegmenterService = ffmpegSegmenterService;
        _entityLocker = entityLocker;
        _logger = logger;
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
        try
        {
            _entityLocker.LockPlayout(playout.Id);

            switch (playout.ProgramSchedulePlayoutType)
            {
                case ProgramSchedulePlayoutType.Block:
                    await _blockPlayoutBuilder.Build(playout, request.Mode, _logger, cancellationToken);
                    break;
                case ProgramSchedulePlayoutType.ExternalJson:
                    await _externalJsonPlayoutBuilder.Build(playout, request.Mode, cancellationToken);
                    break;
                case ProgramSchedulePlayoutType.None:
                case ProgramSchedulePlayoutType.Flood:
                default:
                    await _playoutBuilder.Build(playout, request.Mode, cancellationToken);
                    break;
            }

            // let any active segmenter processes know that the playout has been modified
            // and therefore the segmenter may need to seek into the next item instead of
            // starting at the beginning (if already working ahead)
            bool hasChanges = await dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (request.Mode != PlayoutBuildMode.Continue && hasChanges)
            {
                _ffmpegSegmenterService.PlayoutUpdated(playout.Channel.Number);
            }

            Option<string> maybeChannelNumber = await dbContext.Connection
                .QuerySingleOrDefaultAsync<string>(
                    @"select C.Number from Channel C
                         inner join Playout P on C.Id = P.ChannelId
                         where P.Id = @PlayoutId",
                    new { request.PlayoutId })
                .Map(Optional);

            foreach (string channelNumber in maybeChannelNumber)
            {
                string fileName = Path.Combine(FileSystemLayout.ChannelGuideCacheFolder, $"{channelNumber}.xml");
                if (hasChanges || !File.Exists(fileName) || playout.ProgramSchedulePlayoutType is ProgramSchedulePlayoutType.ExternalJson)
                {
                    await _workerChannel.WriteAsync(new RefreshChannelData(channelNumber), cancellationToken);
                }
            }

            await _workerChannel.WriteAsync(new ExtractEmbeddedSubtitles(playout.Id), cancellationToken);
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            _client.Notify(ex);
            return BaseError.New(
                $"Timeout building playout for channel {playout.Channel.Name}; this may be a bug!");
        }
        catch (Exception ex)
        {
            DebugBreak.Break();

            _client.Notify(ex);
            return BaseError.New(
                $"Unexpected error building playout for channel {playout.Channel.Name}: {ex.Message}");
        }
        finally
        {
            _entityLocker.UnlockPlayout(playout.Id);
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

    private static Task<Validation<BaseError, Playout>> PlayoutMustExist(
        TvContext dbContext,
        BuildPlayout buildPlayout) =>
        dbContext.Playouts
            .Include(p => p.Channel)
            .Include(p => p.Items)
            .Include(p => p.PlayoutHistory)
            .Include(p => p.Templates)
            .ThenInclude(t => t.Template)
            .ThenInclude(t => t.Items)
            .ThenInclude(i => i.Block)
            .ThenInclude(b => b.Items)
            .Include(p => p.FillGroupIndices)
            .ThenInclude(fgi => fgi.EnumeratorState)
            .Include(p => p.ProgramScheduleAlternates)
            .ThenInclude(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.Collection)
            .Include(p => p.ProgramScheduleAlternates)
            .ThenInclude(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.MediaItem)
            .Include(p => p.ProgramScheduleAlternates)
            .ThenInclude(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.PreRollFiller)
            .Include(p => p.ProgramScheduleAlternates)
            .ThenInclude(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.MidRollFiller)
            .Include(p => p.ProgramScheduleAlternates)
            .ThenInclude(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.PostRollFiller)
            .Include(p => p.ProgramScheduleAlternates)
            .ThenInclude(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.TailFiller)
            .Include(p => p.ProgramScheduleAlternates)
            .ThenInclude(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.FallbackFiller)
            .Include(p => p.ProgramScheduleAnchors)
            .ThenInclude(psa => psa.EnumeratorState)
            .Include(p => p.ProgramScheduleAnchors)
            .ThenInclude(a => a.MediaItem)
            .Include(p => p.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.Collection)
            .Include(p => p.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.MediaItem)
            .Include(p => p.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.PreRollFiller)
            .Include(p => p.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.MidRollFiller)
            .Include(p => p.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.PostRollFiller)
            .Include(p => p.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.TailFiller)
            .Include(p => p.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.FallbackFiller)
            .SelectOneAsync(p => p.Id, p => p.Id == buildPlayout.PlayoutId)
            .Map(o => o.ToValidation<BaseError>("Playout does not exist."));
}
