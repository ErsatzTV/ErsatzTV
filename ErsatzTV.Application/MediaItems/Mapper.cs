using System.Globalization;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaItems;

internal static class Mapper
{
    internal static NamedMediaItemViewModel ProjectToViewModel(Show show) =>
        new(show.Id, show.ShowMetadata.HeadOrNone().Map(sm => $"{sm?.Title} ({sm?.Year})").IfNone("???"));

    internal static NamedMediaItemViewModel ProjectToViewModel(Season season) =>
        new(season.Id, $"{ShowTitle(season)} - {SeasonDescription(season)}");

    internal static NamedMediaItemViewModel ProjectToViewModel(Artist artist) =>
        new(artist.Id, artist.ArtistMetadata.HeadOrNone().Match(am => am.Title, () => "???"));

    internal static NamedMediaItemViewModel ProjectToViewModel(Movie movie) =>
        new(movie.Id, MovieTitle(movie));

    internal static NamedMediaItemViewModel ProjectToViewModel(Episode episode) =>
        new(episode.Id, EpisodeTitle(episode));

    internal static NamedMediaItemViewModel ProjectToViewModel(MusicVideo musicVideo) =>
        new(musicVideo.Id, MusicVideoTitle(musicVideo));

    internal static NamedMediaItemViewModel ProjectToViewModel(OtherVideo otherVideo) =>
        new(otherVideo.Id, otherVideo.OtherVideoMetadata.HeadOrNone().Match(ov => ov.Title, () => "???"));
    
    internal static NamedMediaItemViewModel ProjectToViewModel(Song song) =>
        new(song.Id, SongTitle(song));
    
    internal static NamedMediaItemViewModel ProjectToViewModel(Image image) =>
        new(image.Id, image.ImageMetadata.HeadOrNone().Match(i => i.Title, () => "???"));

    private static string MovieTitle(Movie movie)
    {
        var title = "???";
        var year = "???";

        foreach (MovieMetadata movieMetadata in movie.MovieMetadata.HeadOrNone())
        {
            title = movieMetadata.Title;
            foreach (int y in Optional(movieMetadata.Year))
            {
                year = y.ToString(CultureInfo.InvariantCulture);
            }
        }

        return $"{title} ({year})";
    }

    private static string ShowTitle(Season season)
    {
        var title = "???";
        var year = "???";

        foreach (ShowMetadata show in season.Show.ShowMetadata.HeadOrNone())
        {
            title = show.Title;
            foreach (int y in Optional(show.Year))
            {
                year = y.ToString(CultureInfo.InvariantCulture);
            }
        }

        return $"{title} ({year})";
    }

    private static string SeasonDescription(Season season) =>
        season.SeasonNumber == 0 ? "Specials" : $"Season {season.SeasonNumber}";
    
    private static string EpisodeTitle(Episode e)
    {
        string showTitle = e.Season.Show.ShowMetadata.HeadOrNone()
            .Map(sm => $"{sm.Title} - ").IfNone(string.Empty);
        var episodeNumbers = e.EpisodeMetadata.Map(em => em.EpisodeNumber).ToList();
        var episodeTitles = e.EpisodeMetadata.Map(em => em.Title).ToList();
        if (episodeNumbers.Count == 0 || episodeTitles.Count == 0)
        {
            return "[unknown episode]";
        }

        var numbersString = $"e{string.Join('e', episodeNumbers.Map(n => $"{n:00}"))}";
        var titlesString = $"{string.Join('/', episodeTitles)}";

        return $"{showTitle}s{e.Season.SeasonNumber:00}{numbersString} - {titlesString}";
    }

    private static string MusicVideoTitle(MusicVideo mv)
    {
        string artistName = mv.Artist.ArtistMetadata.HeadOrNone()
            .Map(am => $"{am.Title} - ").IfNone(string.Empty);
        return mv.MusicVideoMetadata.HeadOrNone()
            .Map(mvm => $"{artistName}{mvm.Title}")
            .IfNone("[unknown music video]");
    }

    private static string SongTitle(Song s)
    {
        string songArtist = s.SongMetadata.HeadOrNone()
            .Map(sm => $"{string.Join(", ", sm.Artists)} - ")
            .IfNone(string.Empty);
        return s.SongMetadata.HeadOrNone()
            .Map(sm => $"{songArtist}{sm.Title ?? string.Empty}")
            .IfNone("[unknown song]");
    }
}
