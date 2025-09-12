using System.Globalization;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Playouts.Mapper;

namespace ErsatzTV.Application.Playouts;

public class GetFuturePlayoutItemsByIdHandler : IRequestHandler<GetFuturePlayoutItemsById, PagedPlayoutItemsViewModel>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetFuturePlayoutItemsByIdHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<PagedPlayoutItemsViewModel> Handle(
        GetFuturePlayoutItemsById request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        DateTime now = DateTimeOffset.Now.UtcDateTime;

        if (request.ShowFiller)
        {
            List<PlayoutItemViewModel> allItems = await dbContext.PlayoutItems
                .IncludeAllPlayoutItemDetails()
                .Filter(i => i.PlayoutId == request.PlayoutId)
                .Filter(i => i.Finish >= now)
                .OrderBy(i => i.Start)
                .ToListAsync(cancellationToken)
                .Map(list => list.Map(ProjectToViewModel).ToList());
            List<PlayoutItemViewModel> withGaps = InsertUnscheduledItems(allItems);
            int totalCount = withGaps.Count;
            List<PlayoutItemViewModel> finalPage = withGaps.Skip(request.PageNum * request.PageSize).Take(request.PageSize).ToList();
            return new PagedPlayoutItemsViewModel(totalCount, finalPage);
        }
        else
        {
            int totalCount = await dbContext.PlayoutItems
                .CountAsync(
                    i => i.Finish >= now && i.PlayoutId == request.PlayoutId &&
                         i.FillerKind == FillerKind.None,
                    cancellationToken);
            List<PlayoutItemViewModel> page = await dbContext.PlayoutItems
                .IncludeAllPlayoutItemDetails()
                .Filter(i => i.PlayoutId == request.PlayoutId)
                .Filter(i => i.Finish >= now)
                .Filter(i => i.FillerKind == FillerKind.None)
                .OrderBy(i => i.Start)
                .Skip(request.PageNum * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken)
                .Map(list => list.Map(ProjectToViewModel).ToList());
            return new PagedPlayoutItemsViewModel(totalCount, page);
        }
    }

    private static List<PlayoutItemViewModel> InsertUnscheduledItems(List<PlayoutItemViewModel> items)
    {
        List<PlayoutItemViewModel> result = new List<PlayoutItemViewModel>();
        PlayoutItemViewModel prev = null;
        foreach (PlayoutItemViewModel item in items)
        {
            if (prev != null && (item.Start - prev.Finish).TotalSeconds > 2)
            {
                System.TimeSpan gapDuration = item.Start - prev.Finish;
                result.Add(new PlayoutItemViewModel(
                    "UNSCHEDULED",
                    prev.Finish,
                    item.Start,
                    System.TimeSpan.FromSeconds(Math.Round(gapDuration.TotalSeconds)).ToString(gapDuration.TotalHours >= 1 ? @"h\:mm\:ss" :  @"mm\:ss", CultureInfo.CurrentUICulture.DateTimeFormat),
                    None
                ));
            }
            result.Add(item);
            prev = item;
        }
        return result;
    }
}
