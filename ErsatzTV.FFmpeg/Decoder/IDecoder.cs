using ErsatzTV.FFmpeg.Option;

namespace ErsatzTV.FFmpeg.Decoder;

public interface IDecoder : IInputOption
{
    string Name { get; }
}
