using ErsatzTV.FFmpeg.InputOption;

namespace ErsatzTV.FFmpeg.Decoder;

public interface IDecoder : IInputOption
{
    string Name { get; }
}
