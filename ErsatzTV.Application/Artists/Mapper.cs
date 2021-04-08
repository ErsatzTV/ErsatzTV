using System.Linq;
using ErsatzTV.Core.Domain;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Artists
{
    internal static class Mapper
    {
        internal static ArtistViewModel ProjectToViewModel(Artist artist)
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
                metadata.Moods.Map(m => m.Name).ToList());
        }

        private static string Artwork(Metadata metadata, ArtworkKind artworkKind) =>
            Optional(metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == artworkKind))
                .Match(a => a.Path, string.Empty);
    }
}
