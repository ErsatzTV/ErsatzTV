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
}
