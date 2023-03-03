using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata;

public interface IFallbackMetadataProvider
{
    Option<int> GetSeasonNumberForFolder(string folder);
    ShowMetadata GetFallbackMetadataForShow(string showFolder);
    ArtistMetadata GetFallbackMetadataForArtist(string artistFolder);
    List<EpisodeMetadata> GetFallbackMetadata(Episode episode);
    MovieMetadata GetFallbackMetadata(Movie movie);
    Option<MusicVideoMetadata> GetFallbackMetadata(MusicVideo musicVideo);
    Option<OtherVideoMetadata> GetFallbackMetadata(OtherVideo otherVideo);
    Option<SongMetadata> GetFallbackMetadata(Song song);
}
