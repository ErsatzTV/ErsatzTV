using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class ArtworkRepository : IArtworkRepository
    {
        private readonly IDbConnection _dbConnection;

        public ArtworkRepository(IDbConnection dbConnection) => _dbConnection = dbConnection;

        public Task<List<Artwork>> GetOrphanedArtwork() =>
            _dbConnection.QueryAsync<Artwork>(
                    @"SELECT Artwork.Id, Artwork.Path FROM Artwork
                    LEFT OUTER JOIN Actor A on Artwork.Id = A.ArtworkId
                    LEFT OUTER JOIN ArtistMetadata AM on A.ArtistMetadataId = AM.Id
                    LEFT OUTER JOIN EpisodeMetadata EM on A.EpisodeMetadataId = EM.Id
                    LEFT OUTER JOIN SeasonMetadata SM on A.SeasonMetadataId = SM.Id
                    LEFT OUTER JOIN ShowMetadata S on A.ShowMetadataId = S.Id
                    LEFT OUTER JOIN MovieMetadata MM on A.MovieMetadataId = MM.Id
                    LEFT OUTER JOIN MusicVideoMetadata MVM on A.MusicVideoMetadataId = MVM.Id
                    WHERE A.Id IS NULL AND AM.Id IS NULL AND EM.Id IS NULL AND SM.Id IS NULL
                      AND S.Id IS NULL AND MM.Id IS NULL AND MVM.Id IS NULL")
                .Map(result => result.ToList());

        public async Task<Unit> Delete(List<Artwork> artwork)
        {
            IEnumerable<List<int>> chunks = Chunk(artwork.Map(a => a.Id), 100);
            foreach (List<int> chunk in chunks)
            {
                await _dbConnection.ExecuteAsync(
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
}
