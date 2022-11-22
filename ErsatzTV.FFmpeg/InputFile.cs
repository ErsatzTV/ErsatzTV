using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.Option;
using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.FFmpeg;

public abstract record InputFile(string Path, IList<MediaStream> Streams)
{
    public List<IInputOption> InputOptions { get; } = new();
    public List<IPipelineFilterStep> FilterSteps { get; } = new();
}

public record ConcatInputFile(string Url, FrameSize Resolution) : InputFile(
    Url,
    new List<MediaStream>
    {
        new VideoStream(
            0,
            string.Empty,
            Option<IPixelFormat>.None,
            ColorParams.Default,
            Resolution,
            string.Empty,
            string.Empty,
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

public record NullAudioInputFile : AudioInputFile
{
    public NullAudioInputFile(AudioState DesiredState) : base(
        "anullsrc",
        new List<AudioStream> { new(0, "unknown", -1) },
        DesiredState) =>
        InputOptions.Add(new LavfiInputOption());

    public void Deconstruct(out AudioState DesiredState) => DesiredState = this.DesiredState;
}

public record VideoInputFile(string Path, IList<VideoStream> VideoStreams) : InputFile(
    Path,
    VideoStreams.Cast<MediaStream>().ToList())
{
    public void AddOption(IInputOption option)
    {
        if (option.AppliesTo(this))
        {
            InputOptions.Add(option);
        }
    }
}

public record WatermarkInputFile
    (string Path, IList<VideoStream> VideoStreams, WatermarkState DesiredState) : VideoInputFile(Path, VideoStreams);

public record SubtitleInputFile(string Path, IList<MediaStream> SubtitleStreams, bool Copy) : InputFile(
    Path,
    SubtitleStreams)
{
    public bool IsImageBased = SubtitleStreams.All(s => s.Codec is "hdmv_pgs_subtitle" or "dvd_subtitle");
}
