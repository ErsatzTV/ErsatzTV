using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class MediaItemRepository : IMediaItemRepository
{
    private readonly IDbConnection _dbConnection;

    public MediaItemRepository(IDbConnection dbConnection) => _dbConnection = dbConnection;

    public Task<List<string>> GetAllLanguageCodes() =>
        _dbConnection.QueryAsync<string>(
                @"SELECT LanguageCode FROM
                    (SELECT Language AS LanguageCode
                    FROM MediaStream WHERE Language IS NOT NULL
                    UNION ALL SELECT PreferredLanguageCode AS LanguageCode
                    FROM Channel WHERE PreferredLanguageCode IS NOT NULL)
                    GROUP BY LanguageCode
                    ORDER BY COUNT(LanguageCode) DESC")
            .Map(result => result.ToList());

    public async Task<List<int>> FlagFileNotFound(LibraryPath libraryPath, string path)
    {
        List<int> ids = await _dbConnection.QueryAsync<int>(
                @"SELECT M.Id
                FROM MediaItem M
                INNER JOIN MediaVersion MV on M.Id = COALESCE(MovieId, MusicVideoId, OtherVideoId, SongId, EpisodeId)
                INNER JOIN MediaFile MF on MV.Id = MF.MediaVersionId
                WHERE M.LibraryPathId = @LibraryPathId AND MF.Path = @Path",
                new { LibraryPathId = libraryPath.Id, Path = path })
            .Map(result => result.ToList());

        await _dbConnection.ExecuteAsync(
            @"UPDATE MediaItem SET State = 1 WHERE Id IN @Ids",
            new { Ids = ids });

        return ids;
    }

    public async Task<Unit> FlagNormal(MediaItem mediaItem)
    {
        mediaItem.State = MediaItemState.Normal;
            
        return await _dbConnection.ExecuteAsync(
            @"UPDATE MediaItem SET State = 0 WHERE Id = @Id",
            new { mediaItem.Id }).ToUnit();
    }

    public async Task<Either<BaseError, Unit>> DeleteItems(List<int> mediaItemIds)
    {
        foreach (int mediaItemId in mediaItemIds)
        {
            await _dbConnection.ExecuteAsync(
                "DELETE FROM MediaItem WHERE Id = @Id",
                new { Id = mediaItemId });
        }

        return Unit.Default;
    }
}