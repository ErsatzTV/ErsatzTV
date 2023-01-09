using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Playouts.Mapper;

namespace ErsatzTV.Application.Playouts;

public class GetPlayoutAlternateSchedulesHandler :
    IRequestHandler<GetPlayoutAlternateSchedules, List<PlayoutAlternateScheduleViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetPlayoutAlternateSchedulesHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<List<PlayoutAlternateScheduleViewModel>> Handle(
        GetPlayoutAlternateSchedules request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<PlayoutAlternateScheduleViewModel> result = await dbContext.ProgramScheduleAlternates
            .Filter(psa => psa.PlayoutId == request.PlayoutId)
            .Include(psa => psa.ProgramSchedule)
            .Map(psa => ProjectToViewModel(psa))
            .ToListAsync(cancellationToken);

        Option<ProgramSchedule> maybeDefaultSchedule = await dbContext.Playouts
            .Include(p => p.ProgramSchedule)
            .SelectOneAsync(p => p.Id, p => p.Id == request.PlayoutId)
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
                DaysOfMonth = ProgramScheduleAlternate.AllDaysOfMonth(),
                DaysOfWeek = ProgramScheduleAlternate.AllDaysOfWeek(),
                MonthsOfYear = ProgramScheduleAlternate.AllMonthsOfYear()
            };

            result.Add(ProjectToViewModel(psa));
        }

        return result;
    }
}
