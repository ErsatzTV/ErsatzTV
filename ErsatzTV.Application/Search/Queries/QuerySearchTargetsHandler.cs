using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Search;

public class QuerySearchTargetsHandler : IRequestHandler<QuerySearchTargets, List<SearchTargetViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public QuerySearchTargetsHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<List<SearchTargetViewModel>> Handle(
        QuerySearchTargets request,
        CancellationToken cancellationToken)
    {
        var result = new List<SearchTargetViewModel>();

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        result.AddRange(
            dbContext.Channels
                .Map(c => new SearchTargetViewModel(c.Id, c.Name, SearchTargetKind.Channel)));
        result.AddRange(
            dbContext.FFmpegProfiles
                .Map(f => new SearchTargetViewModel(f.Id, f.Name, SearchTargetKind.FFmpegProfile)));
        result.AddRange(
            dbContext.ChannelWatermarks
                .Map(w => new SearchTargetViewModel(w.Id, w.Name, SearchTargetKind.ChannelWatermark)));
        result.AddRange(
            dbContext.Collections
                .Map(c => new SearchTargetViewModel(c.Id, c.Name, SearchTargetKind.Collection)));
        result.AddRange(
            dbContext.MultiCollections
                .Map(mc => new SearchTargetViewModel(mc.Id, mc.Name, SearchTargetKind.MultiCollection)));
        result.AddRange(
            dbContext.SmartCollections
                .Map(sc => new SmartCollectionSearchTargetViewModel(sc.Id, sc.Name, sc.Query)));

        var schedules = await dbContext.ProgramSchedules
            .Map(s => new { s.Id, s.Name })
            .ToListAsync(cancellationToken);

        result.AddRange(
            schedules.SelectMany(
                s => new[]
                {
                    new SearchTargetViewModel(s.Id, s.Name, SearchTargetKind.Schedule),
                    new SearchTargetViewModel(s.Id, s.Name, SearchTargetKind.ScheduleItems)
                }));

        return result;
    }
}
