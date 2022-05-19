namespace ErsatzTV.FFmpeg.Option;

public interface IInputOption : IPipelineStep
{
    bool AppliesTo(AudioInputFile audioInputFile);
    bool AppliesTo(VideoInputFile videoInputFile);
    bool AppliesTo(ConcatInputFile concatInputFile);
}
