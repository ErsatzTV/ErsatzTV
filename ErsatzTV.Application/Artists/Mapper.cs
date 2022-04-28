using System.Globalization;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Artists;

internal static class Mapper
{
    internal static ArtistViewModel ProjectToViewModel(Artist artist, List<string> languages)
    {
        ArtistMetadata metadata = Optional(artist.ArtistMetadata).Flatten().Head();
        return new ArtistViewModel(
            metadata.Title,
            metadata.Disambiguation,
            metadata.Biography,
            Artwork(metadata, ArtworkKind.Thumbnail),
            Artwork(metadata, ArtworkKind.FanArt),
            metadata.Genres.Map(g => g.Name).ToList(),
            metadata.Styles.Map(s => s.Name).ToList(),
            metadata.Moods.Map(m => m.Name).ToList(),
            LanguagesForArtist(languages));
    }

    private static string Artwork(Metadata metadata, ArtworkKind artworkKind) =>
        Optional(metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == artworkKind))
            .Match(a => a.Path, string.Empty);

    private static List<CultureInfo> LanguagesForArtist(List<string> languages)
    {
        CultureInfo[] allCultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);

        return languages
            .Distinct()
            .Map(
                lang => allCultures.Filter(
                    ci => string.Equals(ci.ThreeLetterISOLanguageName, lang, StringComparison.OrdinalIgnoreCase)))
            .Sequence()
            .Flatten()
            .ToList();
    }
}
