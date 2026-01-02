using System.Threading.Channels;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.ProgramSchedules;

public class UpdateProgramScheduleHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    ChannelWriter<IBackgroundServiceRequest> channel)
    :
        IRequestHandler<UpdateProgramSchedule, Either<BaseError, UpdateProgramScheduleResult>>
{
    public async Task<Either<BaseError, UpdateProgramScheduleResult>> Handle(
        UpdateProgramSchedule request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, ProgramSchedule> validation = await Validate(dbContext, request, cancellationToken);
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
            programSchedule.ShuffleScheduleItems != request.ShuffleScheduleItems ||
            programSchedule.RandomStartPoint != request.RandomStartPoint ||
            programSchedule.FixedStartTimeBehavior != request.FixedStartTimeBehavior;

        programSchedule.Name = request.Name;
        programSchedule.KeepMultiPartEpisodesTogether = request.KeepMultiPartEpisodesTogether;
        programSchedule.TreatCollectionsAsShows = programSchedule.KeepMultiPartEpisodesTogether &&
                                                  request.TreatCollectionsAsShows;
        programSchedule.ShuffleScheduleItems = request.ShuffleScheduleItems;
        programSchedule.RandomStartPoint = request.RandomStartPoint;
        programSchedule.FixedStartTimeBehavior = request.FixedStartTimeBehavior;

        await dbContext.SaveChangesAsync();

        if (needToRefreshPlayout)
        {
            List<int> playoutIds = await dbContext.Playouts
                .Filter(p => p.ProgramScheduleId == programSchedule.Id)
                .Map(p => p.Id)
                .ToListAsync();

            foreach (int playoutId in playoutIds)
            {
                await channel.WriteAsync(new BuildPlayout(playoutId, PlayoutBuildMode.Refresh));
            }
        }

        return new UpdateProgramScheduleResult(programSchedule.Id);
    }

    private static async Task<Validation<BaseError, ProgramSchedule>> Validate(
        TvContext dbContext,
        UpdateProgramSchedule request,
        CancellationToken cancellationToken) =>
        (await ProgramScheduleMustExist(dbContext, request, cancellationToken), await ValidateName(dbContext, request, cancellationToken))
        .Apply((programSchedule, _) => programSchedule);

    private static Task<Validation<BaseError, ProgramSchedule>> ProgramScheduleMustExist(
        TvContext dbContext,
        UpdateProgramSchedule request,
        CancellationToken cancellationToken) =>
        dbContext.ProgramSchedules
            .SelectOneAsync(ps => ps.Id, ps => ps.Id == request.ProgramScheduleId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("Schedule does not exist"));

    private static async Task<Validation<BaseError, string>> ValidateName(
        TvContext dbContext,
        UpdateProgramSchedule request,
        CancellationToken cancellationToken)
    {
        Validation<BaseError, string> result1 = request.NotEmpty(c => c.Name)
            .Bind(_ => request.NotLongerThan(50)(c => c.Name));

        bool duplicateName = await dbContext.ProgramSchedules
            .AnyAsync(c => c.Id != request.ProgramScheduleId && c.Name == request.Name, cancellationToken);

        Validation<BaseError, Unit> result2 = duplicateName
            ? Fail<BaseError, Unit>("Schedule name must be unique")
            : Success<BaseError, Unit>(Unit.Default);

        return (result1, result2).Apply((_, _) => request.Name);
    }
}
