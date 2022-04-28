﻿using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class JellyfinCollectionRepository : IJellyfinCollectionRepository
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public JellyfinCollectionRepository(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<List<JellyfinCollection>> GetCollections()
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.JellyfinCollections.ToListAsync();
    }

    public async Task<bool> AddCollection(JellyfinCollection collection)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.AddAsync(collection);
        return await dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> RemoveCollection(JellyfinCollection collection)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        dbContext.Remove(collection);

        // remove all tags that reference this collection
        await dbContext.Connection.ExecuteAsync(
            @"DELETE FROM Tag WHERE Name = @Name AND ExternalCollectionId = @ItemId",
            new { collection.Name, collection.ItemId });

        return await dbContext.SaveChangesAsync() > 0;
    }

    public async Task<List<int>> RemoveAllTags(JellyfinCollection collection)
    {
        var result = new List<int>();

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        // movies
        result.AddRange(
            await dbContext.Connection.QueryAsync<int>(
                @"SELECT JM.Id FROM Tag T
              INNER JOIN MovieMetadata MM on T.MovieMetadataId = MM.Id
              INNER JOIN JellyfinMovie JM on JM.Id = MM.MovieId
              WHERE T.ExternalCollectionId = @ItemId",
                new { collection.ItemId }));

        // shows
        result.AddRange(
            await dbContext.Connection.QueryAsync<int>(
                @"SELECT JS.Id FROM Tag T
              INNER JOIN ShowMetadata SM on T.ShowMetadataId = SM.Id
              INNER JOIN JellyfinShow JS on JS.Id = SM.ShowId
              WHERE T.ExternalCollectionId = @ItemId",
                new { collection.ItemId }));

        // seasons
        result.AddRange(
            await dbContext.Connection.QueryAsync<int>(
                @"SELECT JS.Id FROM Tag T
              INNER JOIN SeasonMetadata SM on T.SeasonMetadataId = SM.Id
              INNER JOIN JellyfinSeason JS on JS.Id = SM.SeasonId
              WHERE T.ExternalCollectionId = @ItemId",
                new { collection.ItemId }));

        // episodes
        result.AddRange(
            await dbContext.Connection.QueryAsync<int>(
                @"SELECT JE.Id FROM Tag T
              INNER JOIN EpisodeMetadata EM on T.EpisodeMetadataId = EM.Id
              INNER JOIN JellyfinEpisode JE on JE.Id = EM.EpisodeId
              WHERE T.ExternalCollectionId = @ItemId",
                new { collection.ItemId }));

        // delete all tags
        await dbContext.Connection.ExecuteAsync(
            @"DELETE FROM Tag WHERE Name = @Name AND ExternalCollectionId = @ItemId",
            new { collection.Name, collection.ItemId });

        return result;
    }

    public async Task<int> AddTag(MediaItem item, JellyfinCollection collection)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        switch (item)
        {
            case JellyfinMovie movie:
                int movieId = await dbContext.Connection.ExecuteScalarAsync<int>(
                    @"SELECT Id FROM JellyfinMovie WHERE ItemId = @ItemId",
                    new { movie.ItemId });
                await dbContext.Connection.ExecuteAsync(
                    @"INSERT INTO Tag (Name, ExternalCollectionId, MovieMetadataId)
                      SELECT @Name, @ItemId, Id FROM
                      (SELECT Id FROM MovieMetadata WHERE MovieId = @MovieId)",
                    new { collection.Name, collection.ItemId, MovieId = movieId });
                return movieId;
            case JellyfinShow show:
                int showId = await dbContext.Connection.ExecuteScalarAsync<int>(
                    @"SELECT Id FROM JellyfinShow WHERE ItemId = @ItemId",
                    new { show.ItemId });
                await dbContext.Connection.ExecuteAsync(
                    @"INSERT INTO Tag (Name, ExternalCollectionId, ShowMetadataId)
                      SELECT @Name, @ItemId, Id FROM
                      (SELECT Id FROM ShowMetadata WHERE ShowId = @ShowId)",
                    new { collection.Name, collection.ItemId, ShowId = showId });
                return showId;
            case JellyfinSeason season:
                int seasonId = await dbContext.Connection.ExecuteScalarAsync<int>(
                    @"SELECT Id FROM JellyfinSeason WHERE ItemId = @ItemId",
                    new { season.ItemId });
                await dbContext.Connection.ExecuteAsync(
                    @"INSERT INTO Tag (Name, ExternalCollectionId, SeasonMetadataId)
                      SELECT @Name, @ItemId, Id FROM
                      (SELECT Id FROM SeasonMetadata WHERE SeasonId = @SeasonId)",
                    new { collection.Name, collection.ItemId, SeasonId = seasonId });
                return seasonId;
            case JellyfinEpisode episode:
                int episodeId = await dbContext.Connection.ExecuteScalarAsync<int>(
                    @"SELECT Id FROM JellyfinEpisode WHERE ItemId = @ItemId",
                    new { episode.ItemId });
                await dbContext.Connection.ExecuteAsync(
                    @"INSERT INTO Tag (Name, ExternalCollectionId, EpisodeMetadataId)
                      SELECT @Name, @ItemId, Id FROM
                      (SELECT Id FROM EpisodeMetadata WHERE EpisodeId = @EpisodeId)",
                    new { collection.Name, collection.ItemId, EpisodeId = episodeId });
                return episodeId;
            default:
                return 0;
        }
    }

    public async Task<bool> SetEtag(JellyfinCollection collection)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            @"UPDATE JellyfinCollection SET Etag = @Etag WHERE ItemId = @ItemId",
            new { collection.Etag, collection.ItemId }) > 0;
    }
}
