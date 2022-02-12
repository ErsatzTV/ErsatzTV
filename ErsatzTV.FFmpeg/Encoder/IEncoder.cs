namespace ErsatzTV.FFmpeg.Encoder;

public interface IEncoder : IPipelineStep
{
    string Name { get; }
    StreamKind Kind { get; }
}
