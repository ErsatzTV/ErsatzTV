using CliWrap;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.Core.Interfaces.FFmpeg;

public interface IMpegTsScriptService
{
    Task RefreshScripts();

    List<MpegTsScript> GetScripts();

    Task<Option<Command>> Execute(MpegTsScript script, Channel channel, string hlsUrl, string ffmpegPath);
}
