using System.Collections.Immutable;
using System.Globalization;
using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class MediaItemRepository : IMediaItemRepository
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public MediaItemRepository(IDbContextFactory<TvContext> dbContextFactory) => _dbContextFactory = dbContextFactory;

    public async Task<List<CultureInfo>> GetAllKnownCultures()
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        var result = new System.Collections.Generic.HashSet<CultureInfo>();

        CultureInfo[] allCultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
        foreach (LanguageCode code in await dbContext.LanguageCodes.ToListAsync())
        {
            Option<CultureInfo> maybeCulture = allCultures.Find(
                c => string.Equals(code.ThreeCode1, c.ThreeLetterISOLanguageName, StringComparison.OrdinalIgnoreCase)
                     || string.Equals(
                         code.ThreeCode2,
                         c.ThreeLetterISOLanguageName,
                         StringComparison.OrdinalIgnoreCase));
            foreach (CultureInfo culture in maybeCulture)
            {
                result.Add(culture);
            }
        }

        return result.ToList();
    }

    public async Task<List<CultureInfo>> GetAllLanguageCodeCultures()
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        var result = new System.Collections.Generic.HashSet<CultureInfo>();

        CultureInfo[] allCultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
        List<string> mediaCodes = await GetAllLanguageCodes();
        foreach (string mediaCode in mediaCodes)
        {
            foreach (string code in await dbContext.LanguageCodes.GetAllLanguageCodes(mediaCode))
            {
                Option<CultureInfo> maybeCulture = allCultures.Find(
                    c => string.Equals(code, c.ThreeLetterISOLanguageName, StringComparison.OrdinalIgnoreCase));
                foreach (CultureInfo culture in maybeCulture)
                {
                    result.Add(culture);
                }
            }
        }

        return result.ToList();
    }

    public async Task<List<int>> FlagFileNotFound(LibraryPath libraryPath, string path)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        List<int> ids = await dbContext.Connection.QueryAsync<int>(
                @"SELECT M.Id
                FROM MediaItem M
                INNER JOIN MediaVersion MV on M.Id = COALESCE(MovieId, MusicVideoId, OtherVideoId, SongId, EpisodeId)
                INNER JOIN MediaFile MF on MV.Id = MF.MediaVersionId
                WHERE M.LibraryPathId = @LibraryPathId AND MF.Path = @Path",
                new { LibraryPathId = libraryPath.Id, Path = path })
            .Map(result => result.ToList());

        await dbContext.Connection.ExecuteAsync(
            @"UPDATE MediaItem SET State = 1 WHERE Id IN @Ids",
            new { Ids = ids });

        return ids;
    }

    public async Task<ImmutableHashSet<string>> GetAllTrashedItems(LibraryPath libraryPath)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<string>(
                @"SELECT MF.Path
                FROM MediaItem M
                INNER JOIN MediaVersion MV on M.Id = COALESCE(MovieId, MusicVideoId, OtherVideoId, SongId, EpisodeId)
                INNER JOIN MediaFile MF on MV.Id = MF.MediaVersionId
                WHERE M.State IN (1,2) AND M.LibraryPathId = @LibraryPathId",
                new { LibraryPathId = libraryPath.Id })
            .Map(list => list.ToImmutableHashSet());
    }

    public async Task<Unit> FlagNormal(MediaItem mediaItem)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        mediaItem.State = MediaItemState.Normal;

        return await dbContext.Connection.ExecuteAsync(
            @"UPDATE MediaItem SET State = 0 WHERE Id = @Id",
            new { mediaItem.Id }).ToUnit();
    }

    public async Task<Either<BaseError, Unit>> DeleteItems(List<int> mediaItemIds)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        foreach (int mediaItemId in mediaItemIds)
        {
            await dbContext.Connection.ExecuteAsync(
                "DELETE FROM MediaItem WHERE Id = @Id",
                new { Id = mediaItemId });
        }

        return Unit.Default;
    }

    public static async Task<bool> MediaFileAlreadyExists(
        MediaItem incoming,
        int libraryPathId,
        TvContext dbContext,
        ILogger logger)
    {
        string path = incoming.GetHeadVersion().MediaFiles.Head().Path;
        return await MediaFileAlreadyExists(path, libraryPathId, dbContext, logger);
    }

    public static async Task<bool> MediaFileAlreadyExists(
        string path,
        int libraryPathId,
        TvContext dbContext,
        ILogger logger)
    {
        Option<int> maybeMediaItemId = await dbContext.Connection
            .QuerySingleOrDefaultAsync<int?>(
                @"select coalesce(EpisodeId, MovieId, MusicVideoId, OtherVideoId, SongId) as MediaItemId
                     from MediaVersion MV
                     inner join MediaFile MF on MV.Id = MF.MediaVersionId
                     where MF.Path = @Path",
                new { Path = path })
            .Map(Optional);

        foreach (int mediaItemId in maybeMediaItemId)
        {
            Option<MediaItem> maybeMediaItem = await dbContext.MediaItems
                .AsNoTracking()
                .Include(mi => mi.LibraryPath)
                .ThenInclude(lp => lp.Library)
                .SelectOneAsync(mi => mi.Id, mi => mi.Id == mediaItemId);

            foreach (MediaItem mediaItem in maybeMediaItem)
            {
                Option<Library> maybeIncomingLibrary = await dbContext.Libraries
                    .Filter(l => l.Paths.Any(p => p.Id == libraryPathId))
                    .SingleOrDefaultAsync()
                    .Map(Optional);

                foreach (Library incomingLibrary in maybeIncomingLibrary)
                {
                    string incomingLibraryType = incomingLibrary switch
                    {
                        PlexLibrary => "Plex Library",
                        EmbyLibrary => "Emby Library",
                        JellyfinLibrary => "Jellyfin Library",
                        _ => "Local Library"
                    };

                    string existingLibraryType = mediaItem.LibraryPath.Library switch
                    {
                        PlexLibrary => "Plex Library",
                        EmbyLibrary => "Emby Library",
                        JellyfinLibrary => "Jellyfin Library",
                        _ => "Local Library"
                    };

                    string libraryName = mediaItem.LibraryPath.Library.Name;
                    logger.LogWarning(
                        "Unable to add media item to {IncomingLibraryType} '{IncomingLibraryName}'; {LibraryType} '{LibraryName}' already contains path {Path}",
                        incomingLibraryType,
                        incomingLibrary.Name,
                        existingLibraryType,
                        libraryName,
                        path);

                    return true;
                }
            }
        }

        return false;
    }

    private async Task<List<string>> GetAllLanguageCodes()
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<string>(
                @"SELECT LanguageCode FROM
                    (SELECT Language AS LanguageCode
                    FROM MediaStream WHERE Language IS NOT NULL
                    UNION ALL SELECT PreferredAudioLanguageCode AS LanguageCode
                    FROM Channel WHERE PreferredAudioLanguageCode IS NOT NULL) AS A
                    GROUP BY LanguageCode
                    ORDER BY COUNT(LanguageCode) DESC")
            .Map(result => result.ToList());
    }
}
