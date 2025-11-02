using EFCore.BulkExtensions;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts;

public class InsertPlayoutGapsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<InsertPlayoutGaps>
{
    public async Task Handle(InsertPlayoutGaps request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var toAdd = new List<PlayoutGap>();

        IOrderedQueryable<PlayoutItem> query = dbContext.PlayoutItems
            .Filter(pi => pi.PlayoutId == request.PlayoutId)
            .OrderBy(i => i.Start);

        var queue = new Queue<PlayoutItem>(query);
        while (queue.Count > 1)
        {
            PlayoutItem one = queue.Dequeue();
            PlayoutItem two = queue.Peek();

            DateTime start = one.Finish;
            DateTime finish = two.Start;

            if (start == finish)
            {
                continue;
            }

            var gap = new PlayoutGap
            {
                PlayoutId = request.PlayoutId,
                Start = start,
                Finish = finish
            };

            toAdd.Add(gap);
        }

        // delete all existing gaps
        await dbContext.PlayoutGaps
            .Where(pg => pg.PlayoutId == request.PlayoutId)
            .ExecuteDeleteAsync(cancellationToken);

        // insert new gaps
        await dbContext.BulkInsertAsync(toAdd, cancellationToken: cancellationToken);
    }
}
