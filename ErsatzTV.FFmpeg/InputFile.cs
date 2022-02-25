using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.Option;
using ErsatzTV.FFmpeg.State;
using LanguageExt;

namespace ErsatzTV.FFmpeg;

public record InputFile(string Path, IList<MediaStream> Streams)
{
    public IList<IInputOption> InputOptions { get; } = new List<IInputOption>();
    public IList<IPipelineFilterStep> FilterSteps { get; } = new List<IPipelineFilterStep>();
}

public record ConcatInputFile(string Url, FrameSize Resolution) : InputFile(
    Url,
    new List<MediaStream>
    {
        new VideoStream(
            0,
            string.Empty,
            Option<IPixelFormat>.None,
            Resolution,
            Option<string>.None,
            false)
    })
{
    public void AddOption(IInputOption option)
    {
        if (option.AppliesTo(this))
        {
            InputOptions.Add(option);
        }
    }
}

public record AudioInputFile(string Path, IList<AudioStream> AudioStreams, AudioState DesiredState) : InputFile(
    Path,
    AudioStreams.Cast<MediaStream>().ToList())
{
    public void AddOption(IInputOption option)
    {
        if (option.AppliesTo(this))
        {
            InputOptions.Add(option);
        }
    }
}

public record VideoInputFile(string Path, IList<VideoStream> VideoStreams) : InputFile(
    Path,
    VideoStreams.Cast<MediaStream>().ToList())
{
    public VideoState DesiredState { get; set; }

    public void AddOption(IInputOption option)
    {
        if (option.AppliesTo(this))
        {
            InputOptions.Add(option);
        }
    }
}
