using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class ArtistRepository : IArtistRepository
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public ArtistRepository(IDbContextFactory<TvContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Option<Artist>> GetArtistByMetadata(int libraryPathId, ArtistMetadata metadata)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        Option<int> maybeId = await dbContext.ArtistMetadata
            .Where(
                s => s.Title == metadata.Title && (metadata.MetadataKind == MetadataKind.Fallback ||
                                                   s.Disambiguation == metadata.Disambiguation))
            .Where(s => s.Artist.LibraryPathId == libraryPathId)
            .SingleOrDefaultAsync()
            .Map(Optional)
            .MapT(sm => sm.ArtistId);

        return await maybeId.Match(
            id =>
            {
                return dbContext.Artists
                    .AsNoTracking()
                    .Include(s => s.ArtistMetadata)
                    .ThenInclude(sm => sm.Artwork)
                    .Include(s => s.ArtistMetadata)
                    .ThenInclude(sm => sm.Genres)
                    .Include(s => s.ArtistMetadata)
                    .ThenInclude(sm => sm.Styles)
                    .Include(s => s.ArtistMetadata)
                    .ThenInclude(sm => sm.Moods)
                    .Include(s => s.LibraryPath)
                    .ThenInclude(lp => lp.Library)
                    .OrderBy(s => s.Id)
                    .SingleOrDefaultAsync(s => s.Id == id)
                    .Map(Optional);
            },
            () => Option<Artist>.None.AsTask());
    }

    public async Task<Either<BaseError, MediaItemScanResult<Artist>>> AddArtist(
        int libraryPathId,
        string artistFolder,
        ArtistMetadata metadata)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        try
        {
            metadata.DateAdded = DateTime.UtcNow;
            metadata.Genres ??= new List<Genre>();
            metadata.Styles ??= new List<Style>();
            metadata.Moods ??= new List<Mood>();
            var artist = new Artist
            {
                LibraryPathId = libraryPathId,
                ArtistMetadata = new List<ArtistMetadata> { metadata }
            };

            await dbContext.Artists.AddAsync(artist);
            await dbContext.SaveChangesAsync();
            await dbContext.Entry(artist).Reference(s => s.LibraryPath).LoadAsync();
            await dbContext.Entry(artist.LibraryPath).Reference(lp => lp.Library).LoadAsync();

            return new MediaItemScanResult<Artist>(artist) { IsAdded = true };
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.Message);
        }
    }

    public async Task<List<int>> DeleteEmptyArtists(LibraryPath libraryPath)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        List<Artist> artists = await dbContext.Artists
            .Filter(a => a.LibraryPathId == libraryPath.Id)
            .Filter(a => a.MusicVideos.Count == 0)
            .ToListAsync();
        var ids = artists.Map(a => a.Id).ToList();
        dbContext.Artists.RemoveRange(artists);
        await dbContext.SaveChangesAsync();
        return ids;
    }

    public async Task<Option<Artist>> GetArtist(int artistId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Artists
            .Include(m => m.ArtistMetadata)
            .ThenInclude(m => m.Artwork)
            .Include(m => m.ArtistMetadata)
            .ThenInclude(m => m.Genres)
            .Include(m => m.ArtistMetadata)
            .ThenInclude(m => m.Styles)
            .Include(m => m.ArtistMetadata)
            .ThenInclude(m => m.Moods)
            .OrderBy(m => m.Id)
            .SingleOrDefaultAsync(m => m.Id == artistId)
            .Map(Optional);
    }

    public async Task<List<ArtistMetadata>> GetArtistsForCards(List<int> ids)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.ArtistMetadata
            .AsNoTracking()
            .Filter(am => ids.Contains(am.ArtistId))
            .Include(am => am.Artist)
            .Include(am => am.Artwork)
            .OrderBy(am => am.SortTitle)
            .ToListAsync();
    }

    public async Task<bool> AddGenre(ArtistMetadata metadata, Genre genre)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "INSERT INTO Genre (Name, ArtistMetadataId) VALUES (@Name, @MetadataId)",
            new { genre.Name, MetadataId = metadata.Id }).Map(result => result > 0);
    }

    public async Task<bool> AddStyle(ArtistMetadata metadata, Style style)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "INSERT INTO Style (Name, ArtistMetadataId) VALUES (@Name, @MetadataId)",
            new { style.Name, MetadataId = metadata.Id }).Map(result => result > 0);
    }

    public async Task<bool> AddMood(ArtistMetadata metadata, Mood mood)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "INSERT INTO Mood (Name, ArtistMetadataId) VALUES (@Name, @MetadataId)",
            new { mood.Name, MetadataId = metadata.Id }).Map(result => result > 0);
    }

    public async Task<List<MusicVideo>> GetArtistItems(int artistId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.MusicVideos
            .AsNoTracking()
            .Include(mv => mv.MusicVideoMetadata)
            .Include(mv => mv.MediaVersions)
            .ThenInclude(mv => mv.Chapters)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(mv => mv.Artist)
            .ThenInclude(a => a.ArtistMetadata)
            .Filter(mv => mv.ArtistId == artistId)
            .ToListAsync();
    }

    public async Task<List<Artist>> GetAllArtists()
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Artists
            .AsNoTracking()
            .Include(a => a.ArtistMetadata)
            .ThenInclude(am => am.Artwork)
            .ToListAsync();
    }
}