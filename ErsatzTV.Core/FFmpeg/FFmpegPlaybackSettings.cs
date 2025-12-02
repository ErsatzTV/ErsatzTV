using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.FFmpeg;
using ErsatzTV.FFmpeg.Format;

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
    public IPixelFormat PixelFormat { get; set; }
    public Option<int> VideoBitrate { get; set; }
    public Option<int> VideoBufferSize { get; set; }
    public FFmpegProfileTonemapAlgorithm TonemapAlgorithm { get; set; }
    public Option<int> AudioBitrate { get; set; }
    public Option<int> AudioBufferSize { get; set; }
    public Option<int> AudioChannels { get; set; }
    public Option<int> AudioSampleRate { get; set; }
    public bool PadAudio { get; set; }
    public FFmpegProfileAudioFormat AudioFormat { get; set; }
    public bool Deinterlace { get; set; }
    public Option<int> VideoTrackTimeScale { get; set; }
    public NormalizeLoudnessMode NormalizeLoudnessMode { get; set; }
    public Option<FrameRate> FrameRate { get; set; }
}
