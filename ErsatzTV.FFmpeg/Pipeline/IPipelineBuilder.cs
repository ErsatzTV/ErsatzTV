namespace ErsatzTV.FFmpeg.Pipeline;

public interface IPipelineBuilder
{
    FFmpegPipeline Resize(string outputFile, FrameSize scaledSize);
    FFmpegPipeline Seek(string inputFile, TimeSpan seek);
    FFmpegPipeline Concat(ConcatInputFile concatInputFile, FFmpegState ffmpegState);
    FFmpegPipeline WrapSegmenter(ConcatInputFile concatInputFile, FFmpegState ffmpegState);
    FFmpegPipeline Build(FFmpegState ffmpegState, FrameState desiredState);
}
