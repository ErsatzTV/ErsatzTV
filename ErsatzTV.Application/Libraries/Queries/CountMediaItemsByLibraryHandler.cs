using Dapper;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Libraries;

public class CountMediaItemsByLibraryHandler : IRequestHandler<CountMediaItemsByLibrary, int>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public CountMediaItemsByLibraryHandler(IDbContextFactory<TvContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<int> Handle(CountMediaItemsByLibrary request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Connection.QuerySingleAsync<int>(
            @"SELECT COUNT(*) FROM MediaItem
                  INNER JOIN LibraryPath LP on MediaItem.LibraryPathId = LP.Id
                  WHERE LP.LibraryId = @LibraryId",
            new { request.LibraryId });
    }
}