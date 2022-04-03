using System.Threading.Channels;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.ProgramSchedules;

public class UpdateProgramScheduleHandler :
    IRequestHandler<UpdateProgramSchedule, Either<BaseError, UpdateProgramScheduleResult>>
{
    private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public UpdateProgramScheduleHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        ChannelWriter<IBackgroundServiceRequest> channel)
    {
        _dbContextFactory = dbContextFactory;
        _channel = channel;
    }

    public async Task<Either<BaseError, UpdateProgramScheduleResult>> Handle(
        UpdateProgramSchedule request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, ProgramSchedule> validation = await Validate(dbContext, request);
        return await validation.Apply(ps => ApplyUpdateRequest(dbContext, ps, request));
    }

    private async Task<UpdateProgramScheduleResult> ApplyUpdateRequest(
        TvContext dbContext,
        ProgramSchedule programSchedule,
        UpdateProgramSchedule request)
    {
        // we need to refresh playouts if the playback order or keep multi-episodes has been modified
        bool needToRefreshPlayout =
            programSchedule.KeepMultiPartEpisodesTogether != request.KeepMultiPartEpisodesTogether ||
            programSchedule.TreatCollectionsAsShows != request.TreatCollectionsAsShows ||
            programSchedule.ShuffleScheduleItems != request.ShuffleScheduleItems;

        programSchedule.Name = request.Name;
        programSchedule.KeepMultiPartEpisodesTogether = request.KeepMultiPartEpisodesTogether;
        programSchedule.TreatCollectionsAsShows = programSchedule.KeepMultiPartEpisodesTogether &&
                                                  request.TreatCollectionsAsShows;
        programSchedule.ShuffleScheduleItems = request.ShuffleScheduleItems;

        await dbContext.SaveChangesAsync();

        if (needToRefreshPlayout)
        {
            List<int> playoutIds = await dbContext.Playouts
                .Filter(p => p.ProgramScheduleId == programSchedule.Id)
                .Map(p => p.Id)
                .ToListAsync();

            foreach (int playoutId in playoutIds)
            {
                await _channel.WriteAsync(new BuildPlayout(playoutId, PlayoutBuildMode.Refresh));
            }
        }

        return new UpdateProgramScheduleResult(programSchedule.Id);
    }

    private static async Task<Validation<BaseError, ProgramSchedule>> Validate(
        TvContext dbContext,
        UpdateProgramSchedule request) =>
        (await ProgramScheduleMustExist(dbContext, request), ValidateName(request))
        .Apply((programSchedule, _) => programSchedule);

    private static Task<Validation<BaseError, ProgramSchedule>> ProgramScheduleMustExist(
        TvContext dbContext,
        UpdateProgramSchedule request) =>
        dbContext.ProgramSchedules
            .SelectOneAsync(ps => ps.Id, ps => ps.Id == request.ProgramScheduleId)
            .Map(o => o.ToValidation<BaseError>("ProgramSchedule does not exist"));

    private static Validation<BaseError, string> ValidateName(UpdateProgramSchedule request) =>
        request.NotEmpty(c => c.Name)
            .Bind(_ => request.NotLongerThan(50)(c => c.Name));
}