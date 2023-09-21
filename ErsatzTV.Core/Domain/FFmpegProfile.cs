using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.Core.Domain;

public record FFmpegProfile
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int ThreadCount { get; set; }
    public HardwareAccelerationKind HardwareAcceleration { get; set; }
    public VaapiDriver VaapiDriver { get; set; }
    public string VaapiDevice { get; set; }
    public int? QsvExtraHardwareFrames { get; set; }
    public int ResolutionId { get; set; }
    public Resolution Resolution { get; set; }
    public ScalingBehavior ScalingBehavior { get; set; }
    public FFmpegProfileVideoFormat VideoFormat { get; set; }
    public FFmpegProfileBitDepth BitDepth { get; set; }
    public int VideoBitrate { get; set; }
    public int VideoBufferSize { get; set; }
    public FFmpegProfileAudioFormat AudioFormat { get; set; }
    public int AudioBitrate { get; set; }
    public int AudioBufferSize { get; set; }
    public NormalizeLoudnessMode NormalizeLoudnessMode { get; set; }
    public int AudioChannels { get; set; }
    public int AudioSampleRate { get; set; }
    public bool NormalizeFramerate { get; set; }
    public bool? DeinterlaceVideo { get; set; }

    public static FFmpegProfile New(string name, Resolution resolution) =>
        new()
        {
            Name = name,
            ThreadCount = 0,
            ResolutionId = resolution.Id,
            Resolution = resolution,
            VideoFormat = FFmpegProfileVideoFormat.H264,
            AudioFormat = FFmpegProfileAudioFormat.Ac3,
            VideoBitrate = 2000,
            VideoBufferSize = 4000,
            AudioBitrate = 192,
            AudioBufferSize = 384,
            NormalizeLoudnessMode = NormalizeLoudnessMode.DynAudNorm,
            AudioChannels = 2,
            AudioSampleRate = 48,
            DeinterlaceVideo = true,
            NormalizeFramerate = false,
            HardwareAcceleration = HardwareAccelerationKind.None,
            QsvExtraHardwareFrames = 64
        };
}
