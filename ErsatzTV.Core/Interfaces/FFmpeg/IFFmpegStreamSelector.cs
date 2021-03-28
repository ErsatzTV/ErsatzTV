using System.Threading.Tasks;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.FFmpeg
{
    public interface IFFmpegStreamSelector
    {
        Task<MediaStream> SelectVideoStream(Channel channel, MediaVersion version);
        Task<MediaStream> SelectAudioStream(Channel channel, MediaVersion version);
    }
}
