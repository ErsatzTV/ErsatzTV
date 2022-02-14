namespace ErsatzTV.FFmpeg.Decoder;

public interface IDecoder : IPipelineStep
{
    string Name { get; }
}
