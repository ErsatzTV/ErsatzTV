using System.Threading.Channels;
using ErsatzTV.Application.Channels;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts;

public class
    UpdateScriptedPlayoutHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        ChannelWriter<IBackgroundServiceRequest> workerChannel)
    : IRequestHandler<UpdateScriptedPlayout,
        Either<BaseError, PlayoutNameViewModel>>
{
    public async Task<Either<BaseError, PlayoutNameViewModel>> Handle(
        UpdateScriptedPlayout request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Playout> validation = await Validate(dbContext, request);
        return await validation.Apply(playout => ApplyUpdateRequest(dbContext, request, playout));
    }

    private async Task<PlayoutNameViewModel> ApplyUpdateRequest(
        TvContext dbContext,
        UpdateScriptedPlayout request,
        Playout playout)
    {
        playout.ScheduleFile = request.ScheduleFile;

        if (await dbContext.SaveChangesAsync() > 0)
        {
            await workerChannel.WriteAsync(new RefreshChannelData(playout.Channel.Number));
        }

        return new PlayoutNameViewModel(
            playout.Id,
            playout.ScheduleKind,
            playout.Channel.Name,
            playout.Channel.Number,
            playout.Channel.PlayoutMode,
            playout.ProgramSchedule?.Name ?? string.Empty,
            playout.ScheduleFile,
            playout.DailyRebuildTime);
    }

    private static Task<Validation<BaseError, Playout>> Validate(
        TvContext dbContext,
        UpdateScriptedPlayout request) =>
        PlayoutMustExist(dbContext, request);

    private static Task<Validation<BaseError, Playout>> PlayoutMustExist(
        TvContext dbContext,
        UpdateScriptedPlayout updatePlayout) =>
        dbContext.Playouts
            .Include(p => p.Channel)
            .SelectOneAsync(p => p.Id, p => p.Id == updatePlayout.PlayoutId)
            .Map(o => o.ToValidation<BaseError>("Playout does not exist."));
}
