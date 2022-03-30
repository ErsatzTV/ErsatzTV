using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class MediaItemRepository : IMediaItemRepository
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public MediaItemRepository(IDbContextFactory<TvContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<string>> GetAllLanguageCodes()
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<string>(
                @"SELECT LanguageCode FROM
                    (SELECT Language AS LanguageCode
                    FROM MediaStream WHERE Language IS NOT NULL
                    UNION ALL SELECT PreferredAudioLanguageCode AS LanguageCode
                    FROM Channel WHERE PreferredAudioLanguageCode IS NOT NULL)
                    GROUP BY LanguageCode
                    ORDER BY COUNT(LanguageCode) DESC")
            .Map(result => result.ToList());
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
}