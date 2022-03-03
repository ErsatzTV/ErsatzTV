using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata;

public interface ILocalMetadataProvider
{
    Task<ShowMetadata> GetMetadataForShow(string showFolder);
    Task<ArtistMetadata> GetMetadataForArtist(string artistFolder);
    Task<bool> RefreshSidecarMetadata(Movie movie, string nfoFileName);
    Task<bool> RefreshSidecarMetadata(Show televisionShow, string nfoFileName);
    Task<bool> RefreshSidecarMetadata(Episode episode, string nfoFileName);
    Task<bool> RefreshSidecarMetadata(Artist artist, string nfoFileName);
    Task<bool> RefreshSidecarMetadata(MusicVideo musicVideo, string nfoFileName);
    Task<bool> RefreshTagMetadata(Song song, string ffprobePath);
    Task<bool> RefreshFallbackMetadata(Movie movie);
    Task<bool> RefreshFallbackMetadata(Episode episode);
    Task<bool> RefreshFallbackMetadata(Artist artist, string artistFolder);
    Task<bool> RefreshFallbackMetadata(MusicVideo musicVideo);
    Task<bool> RefreshFallbackMetadata(OtherVideo otherVideo);
    Task<bool> RefreshFallbackMetadata(Song song);
    Task<bool> RefreshFallbackMetadata(Show televisionShow, string showFolder);
}