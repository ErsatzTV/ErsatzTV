using System.Data;
using System.Threading.Tasks;
using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class MetadataRepository : IMetadataRepository
    {
        private readonly IDbConnection _dbConnection;

        public MetadataRepository(IDbConnection dbConnection) => _dbConnection = dbConnection;

        public Task<Unit> RemoveGenre(Genre genre) =>
            _dbConnection.ExecuteAsync("DELETE FROM Genre WHERE Id = @GenreId", new { GenreId = genre.Id }).ToUnit();

        public Task<Unit> UpdateStatistics(MediaVersion mediaVersion) =>
            _dbConnection.ExecuteAsync(
                @"UPDATE MediaVersion SET
                  SampleAspectRatio = @SampleAspectRatio,
                  VideoScanKind = @VideoScanKind,
                  DateUpdated = @DateUpdated
                  WHERE Id = @MediaVersionId",
                new
                {
                    mediaVersion.SampleAspectRatio,
                    mediaVersion.VideoScanKind,
                    mediaVersion.DateUpdated,
                    MediaVersionId = mediaVersion.Id
                }).ToUnit();

        public Task<Unit> UpdateArtworkPath(Artwork artwork) =>
            _dbConnection.ExecuteAsync(
                "UPDATE Artwork SET Path = @Path, DateUpdated = @DateUpdated WHERE Id = @Id",
                new { artwork.Path, artwork.DateUpdated, artwork.Id }).ToUnit();

        public Task<Unit> AddArtwork(Metadata metadata, Artwork artwork)
        {
            var parameters = new
            {
                artwork.ArtworkKind, metadata.Id, artwork.DateAdded, artwork.DateUpdated, artwork.Path
            };

            return metadata switch
            {
                MovieMetadata => _dbConnection.ExecuteAsync(
                        @"INSERT INTO Artwork (ArtworkKind, MovieMetadataId, DateAdded, DateUpdated, Path)
                            VALUES (@ArtworkKind, @Id, @DateAdded, @DateUpdated, @Path)",
                        parameters)
                    .ToUnit(),
                ShowMetadata => _dbConnection.ExecuteAsync(
                        @"INSERT INTO Artwork (ArtworkKind, ShowMetadataId, DateAdded, DateUpdated, Path)
                            VALUES (@ArtworkKind, @Id, @DateAdded, @DateUpdated, @Path)",
                        parameters)
                    .ToUnit(),
                SeasonMetadata => _dbConnection.ExecuteAsync(
                        @"INSERT INTO Artwork (ArtworkKind, SeasonMetadataId, DateAdded, DateUpdated, Path)
                            VALUES (@ArtworkKind, @Id, @DateAdded, @DateUpdated, @Path)",
                        parameters)
                    .ToUnit(),
                EpisodeMetadata => _dbConnection.ExecuteAsync(
                        @"INSERT INTO Artwork (ArtworkKind, EpisodeMetadataId, DateAdded, DateUpdated, Path)
                            VALUES (@ArtworkKind, @Id, @DateAdded, @DateUpdated, @Path)",
                        parameters)
                    .ToUnit(),
                _ => Task.FromResult(Unit.Default)
            };
        }

        public Task<Unit> RemoveArtwork(Metadata metadata, ArtworkKind artworkKind) =>
            _dbConnection.ExecuteAsync(
                @"DELETE FROM Artwork WHERE ArtworkKind = @ArtworkKind AND (MovieMetadataId = @Id
                OR ShowMetadataId = @Id OR SeasonMetadataId = @Id OR EpisodeMetadataId = @Id)",
                new { ArtworkKind = artworkKind, metadata.Id }).ToUnit();
    }
}
