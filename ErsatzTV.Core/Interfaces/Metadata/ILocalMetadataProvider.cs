using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface ILocalMetadataProvider
    {
        Task<ShowMetadata> GetMetadataForShow(string showFolder);
        Task<Option<MusicVideoMetadata>> GetMetadataForMusicVideo(string filePath);
        Task<bool> RefreshSidecarMetadata(Movie movie, string nfoFileName);
        Task<bool> RefreshSidecarMetadata(Show televisionShow, string nfoFileName);
        Task<bool> RefreshSidecarMetadata(Episode episode, string nfoFileName);
        Task<bool> RefreshSidecarMetadata(MusicVideo musicVideo, string nfoFileName);
        Task<bool> RefreshFallbackMetadata(Movie movie);
        Task<bool> RefreshFallbackMetadata(Episode episode);
        Task<bool> RefreshFallbackMetadata(Show televisionShow, string showFolder);
    }
}
