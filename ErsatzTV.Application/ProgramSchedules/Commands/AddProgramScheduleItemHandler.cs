using System.Threading.Channels;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.ProgramSchedules.Mapper;

namespace ErsatzTV.Application.ProgramSchedules;

public class AddProgramScheduleItemHandler : ProgramScheduleItemCommandBase,
    IRequestHandler<AddProgramScheduleItem, Either<BaseError, ProgramScheduleItemViewModel>>
{
    private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public AddProgramScheduleItemHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        ChannelWriter<IBackgroundServiceRequest> channel)
    {
        _dbContextFactory = dbContextFactory;
        _channel = channel;
    }

    public async Task<Either<BaseError, ProgramScheduleItemViewModel>> Handle(
        AddProgramScheduleItem request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, ProgramSchedule> validation = await Validate(dbContext, request, cancellationToken);
        return await validation.Apply(ps => PersistItem(dbContext, request, ps, cancellationToken));
    }

    private async Task<ProgramScheduleItemViewModel> PersistItem(
        TvContext dbContext,
        AddProgramScheduleItem request,
        ProgramSchedule programSchedule,
        CancellationToken cancellationToken)
    {
        int nextIndex = programSchedule.Items.Select(i => i.Index).DefaultIfEmpty(0).Max() + 1;

        ProgramScheduleItem item = BuildItem(programSchedule, nextIndex, request);
        programSchedule.Items.Add(item);

        await dbContext.SaveChangesAsync(cancellationToken);

        // refresh any playouts that use this schedule
        foreach (Playout playout in programSchedule.Playouts)
        {
            await _channel.WriteAsync(new BuildPlayout(playout.Id, PlayoutBuildMode.Refresh), cancellationToken);
        }

        return ProjectToViewModel(item);
    }

    private static Task<Validation<BaseError, ProgramSchedule>> Validate(
        TvContext dbContext,
        AddProgramScheduleItem request,
        CancellationToken cancellationToken) =>
        ProgramScheduleMustExist(dbContext, request.ProgramScheduleId, cancellationToken)
            .BindT(programSchedule => PlayoutModeMustBeValid(request, programSchedule));
}
