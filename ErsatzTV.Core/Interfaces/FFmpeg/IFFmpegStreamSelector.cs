using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.FFmpeg;

public interface IFFmpegStreamSelector
{
    Task<MediaStream> SelectVideoStream(Channel channel, MediaVersion version);
    Task<Option<MediaStream>> SelectAudioStream(Channel channel, MediaVersion version);
}