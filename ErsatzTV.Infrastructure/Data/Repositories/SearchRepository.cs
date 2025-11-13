using System.Runtime.CompilerServices;
using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class SearchRepository(IDbContextFactory<TvContext> dbContextFactory) : ISearchRepository
{
    public async Task<Option<MediaItem>> GetItemToIndex(int id, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var baseItem = await dbContext.MediaItems
            .AsNoTracking()
            .SingleOrDefaultAsync(mi => mi.Id == id, cancellationToken);

        if (baseItem is null)
        {
            return Option<MediaItem>.None;
        }

        switch (baseItem)
        {
            case Movie:
                return await dbContext.Movies
                    .AsNoTracking()
                    .IncludeForSearch()
                    .AsSplitQuery()
                    .SingleOrDefaultAsync(mi => mi.Id == id, cancellationToken);
            case Episode:
                return await dbContext.Episodes
                    .AsNoTracking()
                    .IncludeForSearch()
                    .AsSplitQuery()
                    .SingleOrDefaultAsync(mi => mi.Id == id, cancellationToken);
            case Season:
                return await dbContext.Seasons
                    .AsNoTracking()
                    .IncludeForSearch()
                    .AsSplitQuery()
                    .SingleOrDefaultAsync(mi => mi.Id == id, cancellationToken);
            case Show:
                return await dbContext.Shows
                    .AsNoTracking()
                    .IncludeForSearch()
                    .AsSplitQuery()
                    .SingleOrDefaultAsync(mi => mi.Id == id, cancellationToken);
            case MusicVideo:
                return await dbContext.MusicVideos
                    .AsNoTracking()
                    .IncludeForSearch()
                    .AsSplitQuery()
                    .SingleOrDefaultAsync(mi => mi.Id == id, cancellationToken);
            case Artist:
                return await dbContext.Artists
                    .AsNoTracking()
                    .IncludeForSearch()
                    .AsSplitQuery()
                    .SingleOrDefaultAsync(mi => mi.Id == id, cancellationToken);
            case OtherVideo:
                return await dbContext.OtherVideos
                    .AsNoTracking()
                    .IncludeForSearch()
                    .AsSplitQuery()
                    .SingleOrDefaultAsync(mi => mi.Id == id, cancellationToken);
            case Song:
                return await dbContext.Songs
                    .AsNoTracking()
                    .IncludeForSearch()
                    .AsSplitQuery()
                    .SingleOrDefaultAsync(mi => mi.Id == id, cancellationToken);
            case Image:
                return await dbContext.Images
                    .AsNoTracking()
                    .IncludeForSearch()
                    .AsSplitQuery()
                    .SingleOrDefaultAsync(mi => mi.Id == id, cancellationToken);
            case RemoteStream:
                return await dbContext.RemoteStreams
                    .AsNoTracking()
                    .IncludeForSearch()
                    .AsSplitQuery()
                    .SingleOrDefaultAsync(mi => mi.Id == id, cancellationToken);
        }

        return Option<MediaItem>.None;
    }

    public async Task<List<string>> GetLanguagesForShow(Show show)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<string>(
            @"SELECT DISTINCT Language
                    FROM MediaStream
                    INNER JOIN MediaVersion MV ON MediaStream.MediaVersionId = MV.Id
                    INNER JOIN Episode E ON MV.EpisodeId = E.Id
                    INNER JOIN Season S ON E.SeasonId = S.Id
                    WHERE MediaStreamKind = 2 AND S.ShowId = @ShowId",
            new { ShowId = show.Id }).Map(result => result.ToList());
    }

    public async Task<List<string>> GetSubLanguagesForShow(Show show)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<string>(
            @"SELECT DISTINCT Language
                    FROM MediaStream
                    INNER JOIN MediaVersion MV ON MediaStream.MediaVersionId = MV.Id
                    INNER JOIN Episode E ON MV.EpisodeId = E.Id
                    INNER JOIN Season S ON E.SeasonId = S.Id
                    WHERE MediaStreamKind = 3 AND S.ShowId = @ShowId",
            new { ShowId = show.Id }).Map(result => result.ToList());
    }

    public async Task<List<string>> GetLanguagesForSeason(Season season)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<string>(
            @"SELECT DISTINCT Language
                    FROM MediaStream
                    INNER JOIN MediaVersion MV ON MediaStream.MediaVersionId = MV.Id
                    INNER JOIN Episode E ON MV.EpisodeId = E.Id
                    WHERE MediaStreamKind = 2 AND E.SeasonId = @SeasonId",
            new { SeasonId = season.Id }).Map(result => result.ToList());
    }

    public async Task<List<string>> GetSubLanguagesForSeason(Season season)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<string>(
            @"SELECT DISTINCT Language
                    FROM MediaStream
                    INNER JOIN MediaVersion MV ON MediaStream.MediaVersionId = MV.Id
                    INNER JOIN Episode E ON MV.EpisodeId = E.Id
                    WHERE MediaStreamKind = 3 AND E.SeasonId = @SeasonId",
            new { SeasonId = season.Id }).Map(result => result.ToList());
    }

    public async Task<List<string>> GetLanguagesForArtist(Artist artist)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<string>(
            @"SELECT DISTINCT Language
                    FROM MediaStream
                    INNER JOIN MediaVersion V ON MediaStream.MediaVersionId = V.Id
                    INNER JOIN MusicVideo MV ON V.MusicVideoId = MV.Id
                    INNER JOIN Artist A on MV.ArtistId = A.Id
                    WHERE MediaStreamKind = 2 AND A.Id = @ArtistId",
            new { ArtistId = artist.Id }).Map(result => result.ToList());
    }

    public async Task<List<string>> GetSubLanguagesForArtist(Artist artist)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<string>(
            @"SELECT DISTINCT Language
                    FROM MediaStream
                    INNER JOIN MediaVersion V ON MediaStream.MediaVersionId = V.Id
                    INNER JOIN MusicVideo MV ON V.MusicVideoId = MV.Id
                    INNER JOIN Artist A on MV.ArtistId = A.Id
                    WHERE MediaStreamKind = 3 AND A.Id = @ArtistId",
            new { ArtistId = artist.Id }).Map(result => result.ToList());
    }

    public async IAsyncEnumerable<MediaItem> GetAllMediaItems(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in GetAllMovies(cancellationToken))
        {
            yield return item;
        }

        await foreach (var item in GetAllShows(cancellationToken))
        {
            yield return item;
        }

        await foreach (var item in GetAllSeasons(cancellationToken))
        {
            yield return item;
        }

        await foreach (var item in GetAllEpisodes(cancellationToken))
        {
            yield return item;
        }

        await foreach (var item in GetAllMusicVideos(cancellationToken))
        {
            yield return item;
        }

        await foreach (var item in GetAllArtists(cancellationToken))
        {
            yield return item;
        }

        await foreach (var item in GetAllOtherVideos(cancellationToken))
        {
            yield return item;
        }

        await foreach (var item in GetAllSongs(cancellationToken))
        {
            yield return item;
        }

        await foreach (var item in GetAllImages(cancellationToken))
        {
            yield return item;
        }

        await foreach (var item in GetAllRemoteStreams(cancellationToken))
        {
            yield return item;
        }
    }

    private async IAsyncEnumerable<Movie> GetAllMovies([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        ConfiguredCancelableAsyncEnumerable<Movie> movies = dbContext.Movies
            .AsNoTracking()
            .IncludeForSearch()
            .AsSplitQuery()
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken);

        await foreach (var movie in movies)
        {
            yield return movie;
        }
    }

    private async IAsyncEnumerable<Show> GetAllShows([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        ConfiguredCancelableAsyncEnumerable<Show> shows = dbContext.Shows
            .AsNoTracking()
            .IncludeForSearch()
            .AsSplitQuery()
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken);

        await foreach (var movie in shows)
        {
            yield return movie;
        }
    }

    private async IAsyncEnumerable<Season> GetAllSeasons([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        ConfiguredCancelableAsyncEnumerable<Season> seasons = dbContext.Seasons
            .AsNoTracking()
            .IncludeForSearch()
            .AsSplitQuery()
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken);

        await foreach (var movie in seasons)
        {
            yield return movie;
        }
    }

    private async IAsyncEnumerable<Episode> GetAllEpisodes([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        ConfiguredCancelableAsyncEnumerable<Episode> episodes = dbContext.Episodes
            .AsNoTracking()
            .IncludeForSearch()
            .AsSplitQuery()
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken);

        await foreach (var movie in episodes)
        {
            yield return movie;
        }
    }

    private async IAsyncEnumerable<MusicVideo> GetAllMusicVideos(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        ConfiguredCancelableAsyncEnumerable<MusicVideo> musicVideos = dbContext.MusicVideos
            .AsNoTracking()
            .IncludeForSearch()
            .AsSplitQuery()
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken);

        await foreach (var movie in musicVideos)
        {
            yield return movie;
        }
    }

    private async IAsyncEnumerable<Artist> GetAllArtists([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        ConfiguredCancelableAsyncEnumerable<Artist> artists = dbContext.Artists
            .AsNoTracking()
            .IncludeForSearch()
            .AsSplitQuery()
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken);

        await foreach (var movie in artists)
        {
            yield return movie;
        }
    }

    private async IAsyncEnumerable<OtherVideo> GetAllOtherVideos(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        ConfiguredCancelableAsyncEnumerable<OtherVideo> otherVideos = dbContext.OtherVideos
            .AsNoTracking()
            .IncludeForSearch()
            .AsSplitQuery()
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken);

        await foreach (var movie in otherVideos)
        {
            yield return movie;
        }
    }

    private async IAsyncEnumerable<Song> GetAllSongs([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        ConfiguredCancelableAsyncEnumerable<Song> songs = dbContext.Songs
            .AsNoTracking()
            .IncludeForSearch()
            .AsSplitQuery()
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken);

        await foreach (var movie in songs)
        {
            yield return movie;
        }
    }

    private async IAsyncEnumerable<Image> GetAllImages([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        ConfiguredCancelableAsyncEnumerable<Image> images = dbContext.Images
            .AsNoTracking()
            .IncludeForSearch()
            .AsSplitQuery()
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken);

        await foreach (var movie in images)
        {
            yield return movie;
        }
    }

    private async IAsyncEnumerable<RemoteStream> GetAllRemoteStreams(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        ConfiguredCancelableAsyncEnumerable<RemoteStream> remoteStreams = dbContext.RemoteStreams
            .AsNoTracking()
            .IncludeForSearch()
            .AsSplitQuery()
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken);

        await foreach (var movie in remoteStreams)
        {
            yield return movie;
        }
    }
}
