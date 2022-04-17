using System.Threading.Channels;
using Bugsnag;
using ErsatzTV.Application.Subtitles;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts;

public class BuildPlayoutHandler : IRequestHandler<BuildPlayout, Either<BaseError, Unit>>
{
    private readonly IClient _client;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IPlayoutBuilder _playoutBuilder;
    private readonly IFFmpegSegmenterService _ffmpegSegmenterService;
    private readonly ChannelWriter<ISubtitleWorkerRequest> _ffmpegWorkerChannel;

    public BuildPlayoutHandler(
        IClient client,
        IDbContextFactory<TvContext> dbContextFactory,
        IPlayoutBuilder playoutBuilder,
        IFFmpegSegmenterService ffmpegSegmenterService,
        ChannelWriter<ISubtitleWorkerRequest> ffmpegWorkerChannel)
    {
        _client = client;
        _dbContextFactory = dbContextFactory;
        _playoutBuilder = playoutBuilder;
        _ffmpegSegmenterService = ffmpegSegmenterService;
        _ffmpegWorkerChannel = ffmpegWorkerChannel;
    }

    public async Task<Either<BaseError, Unit>> Handle(BuildPlayout request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Playout> validation = await Validate(dbContext, request);
        return await validation.Apply(playout => ApplyUpdateRequest(dbContext, request, playout));
    }

    private async Task<Unit> ApplyUpdateRequest(TvContext dbContext, BuildPlayout request, Playout playout)
    {
        try
        {
            await _playoutBuilder.Build(playout, request.Mode);
            if (await dbContext.SaveChangesAsync() > 0)
            {
                _ffmpegSegmenterService.PlayoutUpdated(playout.Channel.Number);
                await _ffmpegWorkerChannel.WriteAsync(new ExtractEmbeddedSubtitles(playout.Id));
            }
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
        }

        return Unit.Default;
    }
    private static Task<Validation<BaseError, Playout>> Validate(TvContext dbContext, BuildPlayout request) =>
        PlayoutMustExist(dbContext, request);

    private static Task<Validation<BaseError, Playout>> PlayoutMustExist(
        TvContext dbContext,
        BuildPlayout buildPlayout) =>
        dbContext.Playouts
            .Include(p => p.Channel)
            .Include(p => p.Items)
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