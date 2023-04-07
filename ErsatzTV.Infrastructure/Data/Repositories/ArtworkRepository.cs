using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class ArtworkRepository : IArtworkRepository
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public ArtworkRepository(IDbContextFactory<TvContext> dbContextFactory) => _dbContextFactory = dbContextFactory;

    public async Task<List<Artwork>> GetOrphanedArtwork()
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<Artwork>(
                @"SELECT A.Id, A.Path FROM Artwork A
                      WHERE A.ArtistMetadataId IS NULL AND A.EpisodeMetadataId IS NULL
                      AND A.MovieMetadataId IS NULL AND A.MusicVideoMetadataId IS NULL
                      AND A.SeasonMetadataId IS NULL AND A.ShowMetadataId IS NULL
                      AND A.SongMetadataId IS NULL AND A.ChannelId IS NULL
                      AND A.OtherVideoMetadataId IS NULL
                      AND NOT EXISTS (SELECT * FROM Actor WHERE Actor.ArtworkId = A.Id)")
            .Map(result => result.ToList());
    }

    public async Task<Unit> Delete(List<Artwork> artwork)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        IEnumerable<List<int>> chunks = Chunk(artwork.Map(a => a.Id), 100);
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
