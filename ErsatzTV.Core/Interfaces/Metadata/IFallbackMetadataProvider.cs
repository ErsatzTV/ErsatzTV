using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata;

public interface IFallbackMetadataProvider
{
    ShowMetadata GetFallbackMetadataForShow(string showFolder);
    ArtistMetadata GetFallbackMetadataForArtist(string artistFolder);
    List<EpisodeMetadata> GetFallbackMetadata(Episode episode);
    MovieMetadata GetFallbackMetadata(Movie movie);
    Option<MusicVideoMetadata> GetFallbackMetadata(MusicVideo musicVideo);
    Option<OtherVideoMetadata> GetFallbackMetadata(OtherVideo otherVideo);
    Option<SongMetadata> GetFallbackMetadata(Song song);
    string GetSortTitle(string title);
}