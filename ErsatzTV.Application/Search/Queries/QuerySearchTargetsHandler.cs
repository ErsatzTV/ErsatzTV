using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Search;

public class QuerySearchTargetsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<QuerySearchTargets, List<SearchTargetViewModel>>
{
    public async Task<List<SearchTargetViewModel>> Handle(
        QuerySearchTargets request,
        CancellationToken cancellationToken)
    {
        var result = new List<SearchTargetViewModel>();

        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IEnumerable<SearchTargetViewModel> channels = await dbContext.Channels
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(c => new SearchTargetViewModel(c.Id, c.Name, SearchTargetKind.Channel)));
        result.AddRange(channels);

        IEnumerable<SearchTargetViewModel> ffmpegProfiles = await dbContext.FFmpegProfiles
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(f => new SearchTargetViewModel(f.Id, f.Name, SearchTargetKind.FFmpegProfile)));
        result.AddRange(ffmpegProfiles);

        IEnumerable<SearchTargetViewModel> channelWatermarks = await dbContext.ChannelWatermarks
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(w => new SearchTargetViewModel(w.Id, w.Name, SearchTargetKind.ChannelWatermark)));
        result.AddRange(channelWatermarks);

        IEnumerable<SearchTargetViewModel> collections = await dbContext.Collections
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(c => new SearchTargetViewModel(c.Id, c.Name, SearchTargetKind.Collection)));
        result.AddRange(collections);

        IEnumerable<SearchTargetViewModel> multiCollections = await dbContext.MultiCollections
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(mc => new SearchTargetViewModel(mc.Id, mc.Name, SearchTargetKind.MultiCollection)));
        result.AddRange(multiCollections);

        IEnumerable<SmartCollectionSearchTargetViewModel> smartCollections = await dbContext.SmartCollections
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(sc => new SmartCollectionSearchTargetViewModel(sc.Id, sc.Name, sc.Query)));
        result.AddRange(smartCollections);

        var schedules = await dbContext.ProgramSchedules
            .Map(s => new { s.Id, s.Name })
            .ToListAsync(cancellationToken);

        result.AddRange(
            schedules.SelectMany(s => new[]
            {
                new SearchTargetViewModel(s.Id, s.Name, SearchTargetKind.Schedule),
                new SearchTargetViewModel(s.Id, s.Name, SearchTargetKind.ScheduleItems)
            }));

        return result;
    }
}
