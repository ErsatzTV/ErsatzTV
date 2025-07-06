using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.ProgramSchedules;

public class GetAllProgramSchedulesHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetAllProgramSchedules, List<ProgramScheduleViewModel>>
{
    public async Task<List<ProgramScheduleViewModel>> Handle(
        GetAllProgramSchedules request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.ProgramSchedules
            .Map(ps => new ProgramScheduleViewModel(
                ps.Id,
                ps.Name,
                ps.KeepMultiPartEpisodesTogether,
                ps.TreatCollectionsAsShows,
                ps.ShuffleScheduleItems,
                ps.RandomStartPoint,
                ps.FixedStartTimeBehavior))
            .ToListAsync(cancellationToken);
    }
}
