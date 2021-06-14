using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaItems
{
    internal static class Mapper
    {
        internal static NamedMediaItemViewModel ProjectToViewModel(Show show) =>
            new(show.Id, show.ShowMetadata.HeadOrNone().Map(sm => $"{sm?.Title} ({sm?.Year})").IfNone("???"));

        internal static NamedMediaItemViewModel ProjectToViewModel(Season season) =>
            new(season.Id, $"{ShowTitle(season)} ({SeasonDescription(season)})");

        internal static NamedMediaItemViewModel ProjectToViewModel(Artist artist) =>
            new(artist.Id, artist.ArtistMetadata.HeadOrNone().Match(am => am.Title, () => "???"));

        private static string ShowTitle(Season season) =>
            season.Show.ShowMetadata.HeadOrNone().Map(sm => sm.Title).IfNone("???");

        private static string SeasonDescription(Season season) =>
            season.SeasonNumber == 0 ? "Specials" : $"Season {season.SeasonNumber}";
    }
}
