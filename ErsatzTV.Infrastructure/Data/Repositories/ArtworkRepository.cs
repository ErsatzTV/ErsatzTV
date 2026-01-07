using Dapper;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class ArtworkRepository(IDbContextFactory<TvContext> dbContextFactory) : IArtworkRepository
{
    public async Task<List<int>> GetOrphanedArtworkIds()
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Artwork
            .TagWithCallSite()
            .Where(a => a.IsMetadataOrphan == true)
            .Where(a => !dbContext.Actors.Any(actor => actor.ArtworkId == a.Id))
            .Select(a => a.Id)
            .ToListAsync();
    }

    public async Task<Unit> Delete(List<int> artworkIds)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync();
        IEnumerable<List<int>> chunks = Chunk(artworkIds, 100);
        foreach (List<int> chunk in chunks)
        {
            await dbContext.Connection.ExecuteAsync(
                "DELETE FROM Artwork WHERE Id IN @Ids",
                new { Ids = chunk });
        }

        return Unit.Default;
    }

    private static IEnumerable<List<T>> Chunk<T>(IEnumerable<T> collection, int size)
    {
        var count = 0;
        var chunk = new List<T>(size);

        foreach (T element in collection)
        {
            if (count++ == size)
            {
                yield return chunk;
                chunk = new List<T>(size);
                count = 1;
            }

            chunk.Add(element);
        }

        yield return chunk;
    }
}
