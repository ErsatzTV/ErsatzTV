using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.ProgramSchedules.Mapper;

namespace ErsatzTV.Application.ProgramSchedules;

public class GetPagedProgramSchedulesHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetPagedProgramSchedules, PagedProgramSchedulesViewModel>
{
    public async Task<PagedProgramSchedulesViewModel> Handle(
        GetPagedProgramSchedules request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        int count = await dbContext.ProgramSchedules.CountAsync(cancellationToken);

        IQueryable<ProgramSchedule> query = dbContext.ProgramSchedules.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            query = query.Where(ps => EF.Functions.Like(ps.Name, $"%{request.Query}%"));
        }

        List<ProgramScheduleViewModel> page = await query
            .OrderBy(ps => ps.Name)
            .Skip(request.PageNum * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new PagedProgramSchedulesViewModel(count, page);
    }
}
