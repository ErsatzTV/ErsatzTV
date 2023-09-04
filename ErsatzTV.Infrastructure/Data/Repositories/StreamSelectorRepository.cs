using Dapper;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Scripting;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class StreamSelectorRepository : IStreamSelectorRepository
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public StreamSelectorRepository(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<EpisodeAudioStreamSelectorData> GetEpisodeData(int episodeId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        dynamic episodeData = await dbContext.Connection.QuerySingleAsync(
            @"select
                SM.Id AS ShowMetadataId,
                SM.Title as ShowTitle,
                S.SeasonNumber,
                EM.EpisodeNumber,
                EM.Id as EpisodeMetadataId
            from EpisodeMetadata EM
            inner join Episode E on EM.EpisodeId = E.Id
            inner join Season S on S.Id = E.SeasonId
            inner join ShowMetadata SM on S.ShowId = SM.ShowId
            where EpisodeId = @Id",
            new { Id = episodeId });

        string[] showGuids = await dbContext.Connection
            .QueryAsync<string>(
                @"SELECT Guid FROM MetadataGuid WHERE ShowMetadataId = @Id",
                new { Id = (int)episodeData.ShowMetadataId })
            .MapT(FormatGuid)
            .Map(result => result.ToArray());

        string[] episodeGuids = await dbContext.Connection
            .QueryAsync<string>(
                @"SELECT Guid FROM MetadataGuid WHERE EpisodeMetadataId = @Id",
                new { Id = (int)episodeData.EpisodeMetadataId })
            .MapT(FormatGuid)
            .Map(result => result.ToArray());

        return new EpisodeAudioStreamSelectorData(
            (string)episodeData.ShowTitle,
            showGuids,
            (int)episodeData.SeasonNumber,
            (int)episodeData.EpisodeNumber,
            episodeGuids);
    }

    public async Task<MovieAudioStreamSelectorData> GetMovieData(int movieId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        dynamic movieData = await dbContext.Connection.QuerySingleAsync(
            @"select
                MM.Id AS MovieMetadataId,
                MM.Title as Title
            from MovieMetadata MM
            where MovieId = @Id",
            new { Id = movieId });

        string[] movieGuids = await dbContext.Connection
            .QueryAsync<string>(
                @"SELECT Guid FROM MetadataGuid WHERE MovieMetadataId = @Id",
                new { Id = (int)movieData.MovieMetadataId })
            .MapT(FormatGuid)
            .Map(result => result.ToArray());

        return new MovieAudioStreamSelectorData(movieData.Title, movieGuids);
    }

    private static string FormatGuid(string guid) => guid.Replace("://", "_");
}
