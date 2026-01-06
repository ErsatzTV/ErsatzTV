using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Playouts.Mapper;

namespace ErsatzTV.Application.Playouts;

public class GetPlayoutAlternateSchedulesHandler(IDbContextFactory<TvContext> dbContextFactory) :
    IRequestHandler<GetPlayoutAlternateSchedules, List<PlayoutAlternateScheduleViewModel>>
{
    public async Task<List<PlayoutAlternateScheduleViewModel>> Handle(
        GetPlayoutAlternateSchedules request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<PlayoutAlternateScheduleViewModel> result = await dbContext.ProgramScheduleAlternates
            .AsNoTracking()
            .Filter(psa => psa.PlayoutId == request.PlayoutId)
            .Include(psa => psa.ProgramSchedule)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        Option<ProgramSchedule> maybeDefaultSchedule = await dbContext.Playouts
            .AsNoTracking()
            .Include(p => p.ProgramSchedule)
            .SelectOneAsync(p => p.Id, p => p.Id == request.PlayoutId, cancellationToken)
            .MapT(p => p.ProgramSchedule);

        foreach (ProgramSchedule defaultSchedule in maybeDefaultSchedule)
        {
            var psa = new ProgramScheduleAlternate
            {
                Id = -1,
                PlayoutId = request.PlayoutId,
                ProgramScheduleId = defaultSchedule.Id,
                ProgramSchedule = defaultSchedule,
                Index = result.Map(i => i.Index).DefaultIfEmpty().Max() + 1,
                DaysOfMonth = AlternateScheduleSelector.AllDaysOfMonth(),
                DaysOfWeek = AlternateScheduleSelector.AllDaysOfWeek(),
                MonthsOfYear = AlternateScheduleSelector.AllMonthsOfYear(),
                LimitToDateRange = false,
                StartMonth = 1,
                StartDay = 1,
                StartYear = null,
                EndMonth = 12,
                EndDay = 31,
                EndYear = null
            };

            result.Add(ProjectToViewModel(psa));
        }

        return result;
    }
}
