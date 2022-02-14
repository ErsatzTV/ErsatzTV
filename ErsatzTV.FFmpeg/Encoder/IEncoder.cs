namespace ErsatzTV.FFmpeg.Encoder;

public interface IEncoder : IPipelineFilterStep
{
    string Name { get; }
    StreamKind Kind { get; }
}
