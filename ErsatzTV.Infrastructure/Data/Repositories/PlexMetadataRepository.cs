using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class PlexMetadataRepository(IDbContextFactory<TvContext> dbContextFactory) : IPlexMetadataRepository
{
    public async Task<Unit> RemoveArtwork(Core.Domain.Metadata metadata, ArtworkKind artworkKind)
    {
        // this is only used by plex, so only needs to support plex media kinds (movie, show, season, episode, other video)
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            @"DELETE FROM Artwork WHERE ArtworkKind = @ArtworkKind AND (MovieMetadataId = @Id
                OR ShowMetadataId = @Id OR SeasonMetadataId = @Id OR EpisodeMetadataId = @Id OR OtherVideoMetadataId = @Id)",
            new { ArtworkKind = artworkKind, metadata.Id }).ToUnit();
    }
}
