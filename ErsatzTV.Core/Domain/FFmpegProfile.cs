using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.Core.Domain;

public record FFmpegProfile
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int ThreadCount { get; set; }
    public bool NormalizeAudio { get; set; }
    public bool NormalizeVideo { get; set; }
    public HardwareAccelerationKind HardwareAcceleration { get; set; }
    public string VaapiDisplay { get; set; }
    public VaapiDriver VaapiDriver { get; set; }
    public string VaapiDevice { get; set; }
    public int? QsvExtraHardwareFrames { get; set; }
    public int ResolutionId { get; set; }
    public Resolution Resolution { get; set; }
    public ScalingBehavior ScalingBehavior { get; set; }
    public FilterMode PadMode { get; set; }
    public FFmpegProfileVideoFormat VideoFormat { get; set; }
    public string VideoProfile { get; set; }
    public string VideoPreset { get; set; }
    public bool AllowBFrames { get; set; }
    public FFmpegProfileBitDepth BitDepth { get; set; }
    public int VideoBitrate { get; set; }
    public int VideoBufferSize { get; set; }
    public FFmpegProfileTonemapAlgorithm TonemapAlgorithm { get; set; }
    public FFmpegProfileAudioFormat AudioFormat { get; set; }
    public int AudioBitrate { get; set; }
    public int AudioBufferSize { get; set; }
    public NormalizeLoudnessMode NormalizeLoudnessMode { get; set; }
    public double? TargetLoudness { get; set; }
    public int AudioChannels { get; set; }
    public int AudioSampleRate { get; set; }
    public bool NormalizeFramerate { get; set; }
    public bool NormalizeColors { get; set; }
    public bool? DeinterlaceVideo { get; set; }

    public static FFmpegProfile New(string name, Resolution resolution) =>
        new()
        {
            Name = name,
            ThreadCount = 0,
            ResolutionId = resolution.Id,
            Resolution = resolution,
            ScalingBehavior = ScalingBehavior.ScaleAndPad,
            PadMode = FilterMode.Software,
            VideoFormat = FFmpegProfileVideoFormat.H264,
            VideoProfile = "high",
            VideoPreset = ErsatzTV.FFmpeg.Preset.VideoPreset.Unset,
            AllowBFrames = false,
            AudioFormat = FFmpegProfileAudioFormat.Aac,
            VideoBitrate = 2000,
            VideoBufferSize = 4000,
            TonemapAlgorithm = FFmpegProfileTonemapAlgorithm.Linear,
            AudioBitrate = 192,
            AudioBufferSize = 384,
            NormalizeLoudnessMode = NormalizeLoudnessMode.Off,
            AudioChannels = 2,
            AudioSampleRate = 48,
            DeinterlaceVideo = true,
            NormalizeFramerate = false,
            HardwareAcceleration = HardwareAccelerationKind.None,
            QsvExtraHardwareFrames = 64,
            NormalizeAudio = true,
            NormalizeVideo = true,
            NormalizeColors = true
        };
}
