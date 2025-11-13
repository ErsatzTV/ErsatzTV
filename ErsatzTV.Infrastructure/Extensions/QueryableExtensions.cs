using System.Linq.Expressions;
using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Extensions;

public static class QueryableExtensions
{
    public static async Task<Option<T>> SelectOneAsync<T, TKey>(
        this IQueryable<T> enumerable,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken) where T : class =>
        await enumerable.OrderBy(keySelector).FirstOrDefaultAsync(predicate, cancellationToken);

    public static IQueryable<Movie> IncludeForSearch(this IQueryable<Movie> movies) =>
        movies
            .Include(mi => mi.Collections)
            .Include(mi => mi.LibraryPath).ThenInclude(lp => lp.Library)
            .Include(mi => mi.TraktListItems).ThenInclude(tli => tli.TraktList)
            .Include(m => m.MovieMetadata).ThenInclude(mm => mm.Genres)
            .Include(m => m.MovieMetadata).ThenInclude(mm => mm.Tags)
            .Include(m => m.MovieMetadata).ThenInclude(mm => mm.Studios)
            .Include(m => m.MovieMetadata).ThenInclude(mm => mm.Actors)
            .Include(m => m.MovieMetadata).ThenInclude(mm => mm.Directors)
            .Include(m => m.MovieMetadata).ThenInclude(mm => mm.Writers)
            .Include(m => m.MovieMetadata).ThenInclude(mm => mm.Guids)
            .Include(m => m.MediaVersions).ThenInclude(mv => mv.Chapters)
            .Include(m => m.MediaVersions).ThenInclude(mv => mv.Streams)
            .Include(m => m.MediaVersions).ThenInclude(mv => mv.MediaFiles);

    public static IQueryable<Episode> IncludeForSearch(this IQueryable<Episode> episodes) =>
        episodes
            .Include(mi => mi.Collections)
            .Include(mi => mi.LibraryPath).ThenInclude(lp => lp.Library)
            .Include(mi => mi.TraktListItems).ThenInclude(tli => tli.TraktList)
            .Include(m => m.EpisodeMetadata).ThenInclude(mm => mm.Genres)
            .Include(m => m.EpisodeMetadata).ThenInclude(mm => mm.Tags)
            .Include(m => m.EpisodeMetadata).ThenInclude(mm => mm.Studios)
            .Include(m => m.EpisodeMetadata).ThenInclude(mm => mm.Actors)
            .Include(m => m.EpisodeMetadata).ThenInclude(mm => mm.Directors)
            .Include(m => m.EpisodeMetadata).ThenInclude(mm => mm.Writers)
            .Include(m => m.EpisodeMetadata).ThenInclude(mm => mm.Guids)
            .Include(m => m.MediaVersions).ThenInclude(mv => mv.Chapters)
            .Include(m => m.MediaVersions).ThenInclude(mv => mv.Streams)
            .Include(m => m.MediaVersions).ThenInclude(mv => mv.MediaFiles)
            .Include(m => m.Season).ThenInclude(s => s.Show).ThenInclude(s => s.ShowMetadata).ThenInclude(s => s.Genres)
            .Include(m => m.Season).ThenInclude(s => s.Show).ThenInclude(s => s.ShowMetadata).ThenInclude(s => s.Tags)
            .Include(m => m.Season).ThenInclude(s => s.Show).ThenInclude(s => s.ShowMetadata).ThenInclude(s => s.Studios);

    public static IQueryable<Season> IncludeForSearch(this IQueryable<Season> seasons) =>
        seasons
            .Include(mi => mi.Collections)
            .Include(mi => mi.LibraryPath).ThenInclude(lp => lp.Library)
            .Include(mi => mi.TraktListItems).ThenInclude(tli => tli.TraktList)
            .Include(m => m.SeasonMetadata).ThenInclude(mm => mm.Genres)
            .Include(m => m.SeasonMetadata).ThenInclude(mm => mm.Tags)
            .Include(m => m.SeasonMetadata).ThenInclude(mm => mm.Studios)
            .Include(m => m.SeasonMetadata).ThenInclude(mm => mm.Actors)
            .Include(m => m.SeasonMetadata).ThenInclude(mm => mm.Guids)
            .Include(s => s.Show).ThenInclude(s => s.ShowMetadata).ThenInclude(s => s.Genres)
            .Include(s => s.Show).ThenInclude(s => s.ShowMetadata).ThenInclude(s => s.Tags)
            .Include(s => s.Show).ThenInclude(s => s.ShowMetadata).ThenInclude(s => s.Studios);

    public static IQueryable<Show> IncludeForSearch(this IQueryable<Show> shows) =>
        shows
            .Include(mi => mi.Collections)
            .Include(mi => mi.LibraryPath).ThenInclude(lp => lp.Library)
            .Include(mi => mi.TraktListItems).ThenInclude(tli => tli.TraktList)
            .Include(m => m.ShowMetadata).ThenInclude(mm => mm.Genres)
            .Include(m => m.ShowMetadata).ThenInclude(mm => mm.Tags)
            .Include(m => m.ShowMetadata).ThenInclude(mm => mm.Studios)
            .Include(m => m.ShowMetadata).ThenInclude(mm => mm.Actors)
            .Include(m => m.ShowMetadata).ThenInclude(mm => mm.Guids);

    public static IQueryable<MusicVideo> IncludeForSearch(this IQueryable<MusicVideo> musicVideos) =>
        musicVideos
            .Include(mi => mi.Collections)
            .Include(mi => mi.LibraryPath).ThenInclude(lp => lp.Library)
            .Include(m => m.Artist).ThenInclude(a => a.ArtistMetadata)
            .Include(m => m.MusicVideoMetadata).ThenInclude(mm => mm.Artists)
            .Include(m => m.MusicVideoMetadata).ThenInclude(mm => mm.Genres)
            .Include(m => m.MusicVideoMetadata).ThenInclude(mm => mm.Tags)
            .Include(m => m.MusicVideoMetadata).ThenInclude(mm => mm.Studios)
            .Include(m => m.MusicVideoMetadata).ThenInclude(mm => mm.Guids)
            .Include(m => m.MediaVersions).ThenInclude(mv => mv.Chapters)
            .Include(m => m.MediaVersions).ThenInclude(mv => mv.Streams)
            .Include(m => m.MediaVersions).ThenInclude(mv => mv.MediaFiles);

    public static IQueryable<Artist> IncludeForSearch(this IQueryable<Artist> artists) =>
        artists
            .Include(mi => mi.Collections)
            .Include(mi => mi.LibraryPath).ThenInclude(lp => lp.Library)
            .Include(m => m.ArtistMetadata).ThenInclude(mm => mm.Genres)
            .Include(m => m.ArtistMetadata).ThenInclude(mm => mm.Styles)
            .Include(m => m.ArtistMetadata).ThenInclude(mm => mm.Moods)
            .Include(m => m.ArtistMetadata).ThenInclude(mm => mm.Guids);

    public static IQueryable<OtherVideo> IncludeForSearch(this IQueryable<OtherVideo> otherVideos) =>
        otherVideos
            .Include(mi => mi.Collections)
            .Include(mi => mi.LibraryPath).ThenInclude(lp => lp.Library)
            .Include(m => m.OtherVideoMetadata).ThenInclude(mm => mm.Genres)
            .Include(m => m.OtherVideoMetadata).ThenInclude(mm => mm.Tags)
            .Include(m => m.OtherVideoMetadata).ThenInclude(mm => mm.Studios)
            .Include(m => m.OtherVideoMetadata).ThenInclude(mm => mm.Actors)
            .Include(m => m.OtherVideoMetadata).ThenInclude(mm => mm.Directors)
            .Include(m => m.OtherVideoMetadata).ThenInclude(mm => mm.Writers)
            .Include(m => m.OtherVideoMetadata).ThenInclude(mm => mm.Guids)
            .Include(m => m.MediaVersions).ThenInclude(mv => mv.Chapters)
            .Include(m => m.MediaVersions).ThenInclude(mv => mv.Streams)
            .Include(m => m.MediaVersions).ThenInclude(mv => mv.MediaFiles);

    public static IQueryable<Song> IncludeForSearch(this IQueryable<Song> songs) =>
        songs
            .Include(mi => mi.Collections)
            .Include(mi => mi.LibraryPath).ThenInclude(lp => lp.Library)
            .Include(m => m.SongMetadata).ThenInclude(mm => mm.Tags)
            .Include(m => m.SongMetadata).ThenInclude(mm => mm.Genres)
            .Include(m => m.SongMetadata).ThenInclude(mm => mm.Guids)
            .Include(m => m.MediaVersions).ThenInclude(mv => mv.Chapters)
            .Include(m => m.MediaVersions).ThenInclude(mv => mv.Streams)
            .Include(m => m.MediaVersions).ThenInclude(mv => mv.MediaFiles);

    public static IQueryable<Image> IncludeForSearch(this IQueryable<Image> images) =>
        images
            .Include(mi => mi.Collections)
            .Include(mi => mi.LibraryPath).ThenInclude(lp => lp.Library)
            .Include(m => m.ImageMetadata).ThenInclude(mm => mm.Tags)
            .Include(m => m.ImageMetadata).ThenInclude(mm => mm.Genres)
            .Include(m => m.ImageMetadata).ThenInclude(mm => mm.Guids)
            .Include(m => m.MediaVersions).ThenInclude(mv => mv.Chapters)
            .Include(m => m.MediaVersions).ThenInclude(mv => mv.Streams)
            .Include(m => m.MediaVersions).ThenInclude(mv => mv.MediaFiles);

    public static IQueryable<RemoteStream> IncludeForSearch(this IQueryable<RemoteStream> images) =>
        images
            .Include(mi => mi.Collections)
            .Include(mi => mi.LibraryPath).ThenInclude(lp => lp.Library)
            .Include(m => m.RemoteStreamMetadata).ThenInclude(mm => mm.Tags)
            .Include(m => m.RemoteStreamMetadata).ThenInclude(mm => mm.Genres)
            .Include(m => m.RemoteStreamMetadata).ThenInclude(mm => mm.Guids)
            .Include(m => m.MediaVersions).ThenInclude(mv => mv.Chapters)
            .Include(m => m.MediaVersions).ThenInclude(mv => mv.Streams)
            .Include(m => m.MediaVersions).ThenInclude(mv => mv.MediaFiles);
}
