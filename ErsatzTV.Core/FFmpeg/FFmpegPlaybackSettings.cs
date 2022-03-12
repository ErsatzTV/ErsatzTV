using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Core.FFmpeg;

public class FFmpegPlaybackSettings
{
    public int ThreadCount { get; set; }
    public List<string> FormatFlags { get; set; }
    public HardwareAccelerationKind HardwareAcceleration { get; set; }
    public string VideoDecoder { get; set; }
    public bool RealtimeOutput { get; set; }
    public Option<TimeSpan> StreamSeek { get; set; }
    public Option<IDisplaySize> ScaledSize { get; set; }
    public bool PadToDesiredResolution { get; set; }
    public FFmpegProfileVideoFormat VideoFormat { get; set; }
    public Option<int> VideoBitrate { get; set; }
    public Option<int> VideoBufferSize { get; set; }
    public Option<int> AudioBitrate { get; set; }
    public Option<int> AudioBufferSize { get; set; }
    public Option<int> AudioChannels { get; set; }
    public Option<int> AudioSampleRate { get; set; }
    public Option<TimeSpan> AudioDuration { get; set; }
    public FFmpegProfileAudioFormat AudioFormat { get; set; }
    public bool Deinterlace { get; set; }
    public Option<int> VideoTrackTimeScale { get; set; }
    public bool NormalizeLoudness { get; set; }
    public Option<int> FrameRate { get; set; }
}