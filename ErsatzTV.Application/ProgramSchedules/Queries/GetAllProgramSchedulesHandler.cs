using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.ProgramSchedules;

public class GetAllProgramSchedulesHandler : IRequestHandler<GetAllProgramSchedules, List<ProgramScheduleViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetAllProgramSchedulesHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<List<ProgramScheduleViewModel>> Handle(
        GetAllProgramSchedules request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        return await dbContext.ProgramSchedules
            .Map(
                ps => new ProgramScheduleViewModel(
                    ps.Id,
                    ps.Name,
                    ps.KeepMultiPartEpisodesTogether,
                    ps.TreatCollectionsAsShows,
                    ps.ShuffleScheduleItems,
                    ps.RandomStartPoint))
            .ToListAsync(cancellationToken);
    }
}
