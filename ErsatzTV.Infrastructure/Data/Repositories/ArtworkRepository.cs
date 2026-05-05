using Dapper;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class ArtworkRepository(IDbContextFactory<TvContext> dbContextFactory, ILogger<ArtworkRepository> logger) : IArtworkRepository
{
    public async Task<int> DeleteOrphanedActors(int? max, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        const string ORPHANED_ACTOR_QUERY =
            """
            SELECT Id FROM Actor
            WHERE ArtistMetadataId IS NULL
              AND EpisodeMetadataId IS NULL
              AND ImageMetadataId IS NULL
              AND MovieMetadataId IS NULL
              AND MusicVideoMetadataId IS NULL
              AND OtherVideoMetadataId IS NULL
              AND RemoteStreamMetadataId IS NULL
              AND SeasonMetadataId IS NULL
              AND ShowMetadataId IS NULL
              AND SongMetadataId IS NULL
            LIMIT 5000
            """;

        var totalDeleted = 0;
        while (true)
        {
            List<int> ids = (await dbContext.Connection.QueryAsync<int>(ORPHANED_ACTOR_QUERY)).ToList();
            if (ids.Count == 0 || totalDeleted >= max)
            {
                break;
            }

            if (totalDeleted > 0)
            {
                logger.LogDebug("Deleted {Count} orphaned actors; still have more to delete...", totalDeleted);
            }

            foreach (List<int> chunk in Chunk(ids, 100))
            {
                await dbContext.Connection.ExecuteAsync(
                    new CommandDefinition("DELETE FROM Actor WHERE Id IN @Ids",
                        parameters: new { Ids = chunk },
                        cancellationToken: cancellationToken));
            }

            totalDeleted += ids.Count;
        }

        return totalDeleted;
    }

    public async Task<int> DeleteOrphanedArtwork(int? max, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var totalDeleted = 0;
        while (true)
        {
            IQueryable<int> query = dbContext.Artwork
                .TagWithCallSite()
                .Where(a => a.IsMetadataOrphan == true)
                .Where(a => !dbContext.Actors.Any(actor => actor.ArtworkId == a.Id))
                .OrderBy(a => a.Id)
                .Take(5000)
                .Select(a => a.Id);

            List<int> ids = await query.ToListAsync(cancellationToken);
            if (ids.Count == 0 || totalDeleted >= max)
            {
                break;
            }

            if (totalDeleted > 0)
            {
                logger.LogDebug("Deleted {Count} orphaned artwork; still have more to delete...", totalDeleted);
            }

            foreach (List<int> chunk in Chunk(ids, 100))
            {
                await dbContext.Connection.ExecuteAsync(
                    new CommandDefinition("DELETE FROM Artwork WHERE Id IN @Ids",
                        parameters: new { Ids = chunk },
                        cancellationToken: cancellationToken));
            }

            totalDeleted += ids.Count;
        }

        return totalDeleted;
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
