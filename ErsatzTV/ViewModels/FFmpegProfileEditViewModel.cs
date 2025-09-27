using ErsatzTV.Application.FFmpegProfiles;
using ErsatzTV.Application.Resolutions;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.ViewModels;

public class FFmpegProfileEditViewModel
{
    private string _videoProfile;

    public FFmpegProfileEditViewModel()
    {
    }

    public FFmpegProfileEditViewModel(FFmpegProfileViewModel viewModel)
    {
        AudioBitrate = viewModel.AudioBitrate;
        AudioBufferSize = viewModel.AudioBufferSize;
        AudioChannels = viewModel.AudioChannels;
        AudioFormat = viewModel.AudioFormat;
        AudioSampleRate = viewModel.AudioSampleRate;
        NormalizeLoudnessMode = viewModel.NormalizeLoudnessMode;
        Id = viewModel.Id;
        Name = viewModel.Name;
        NormalizeFramerate = viewModel.NormalizeFramerate;
        DeinterlaceVideo = viewModel.DeinterlaceVideo;
        Resolution = viewModel.Resolution;
        ScalingBehavior = viewModel.ScalingBehavior;
        ThreadCount = viewModel.ThreadCount;
        HardwareAcceleration = viewModel.HardwareAcceleration;
        VaapiDisplay = viewModel.VaapiDisplay;
        VaapiDriver = viewModel.VaapiDriver;
        VaapiDevice = viewModel.VaapiDevice;
        QsvExtraHardwareFrames = viewModel.QsvExtraHardwareFrames;
        VideoBitrate = viewModel.VideoBitrate;
        VideoBufferSize = viewModel.VideoBufferSize;
        VideoFormat = viewModel.VideoFormat;
        VideoProfile = viewModel.VideoProfile;
        VideoPreset = viewModel.VideoPreset;
        AllowBFrames = viewModel.AllowBFrames;
        BitDepth = viewModel.BitDepth;
        TonemapAlgorithm = viewModel.TonemapAlgorithm;
    }

    public int AudioBitrate { get; set; }
    public int AudioBufferSize { get; set; }
    public int AudioChannels { get; set; }
    public FFmpegProfileAudioFormat AudioFormat { get; set; }
    public int AudioSampleRate { get; set; }
    public NormalizeLoudnessMode NormalizeLoudnessMode { get; set; }
    public int Id { get; set; }
    public string Name { get; set; }
    public bool NormalizeFramerate { get; set; }
    public bool DeinterlaceVideo { get; set; }
    public ResolutionViewModel Resolution { get; set; }
    public ScalingBehavior ScalingBehavior { get; set; }
    public int ThreadCount { get; set; }
    public HardwareAccelerationKind HardwareAcceleration { get; set; }
    public string VaapiDisplay { get; set; }
    public VaapiDriver VaapiDriver { get; set; }
    public string VaapiDevice { get; set; }
    public int? QsvExtraHardwareFrames { get; set; }
    public int VideoBitrate { get; set; }
    public int VideoBufferSize { get; set; }
    public FFmpegProfileVideoFormat VideoFormat { get; set; }

    public string VideoProfile
    {
        get =>
            (HardwareAcceleration, VideoFormat, BitDepth) switch
            {
                (HardwareAccelerationKind.Nvenc, FFmpegProfileVideoFormat.H264, FFmpegProfileBitDepth.TenBit) => FFmpeg
                    .Format.VideoProfile.High444p,
                (_, FFmpegProfileVideoFormat.H264, _) => _videoProfile,
                _ => string.Empty
            };
        set => _videoProfile = value;
    }

    public string VideoPreset { get; set; }
    public bool AllowBFrames { get; set; }
    public FFmpegProfileBitDepth BitDepth { get; set; }
    public FFmpegProfileTonemapAlgorithm TonemapAlgorithm { get; set; }

    public CreateFFmpegProfile ToCreate() =>
        new(
            Name,
            ThreadCount,
            HardwareAcceleration,
            VaapiDisplay,
            VaapiDriver,
            VaapiDevice,
            QsvExtraHardwareFrames,
            Resolution.Id,
            ScalingBehavior,
            VideoFormat,
            VideoProfile,
            VideoPreset,
            AllowBFrames,
            BitDepth,
            VideoBitrate,
            VideoBufferSize,
            TonemapAlgorithm,
            AudioFormat,
            AudioBitrate,
            AudioBufferSize,
            NormalizeLoudnessMode,
            AudioChannels,
            AudioSampleRate,
            NormalizeFramerate,
            DeinterlaceVideo
        );

    public UpdateFFmpegProfile ToUpdate() =>
        new(
            Id,
            Name,
            ThreadCount,
            HardwareAcceleration,
            VaapiDisplay,
            VaapiDriver,
            VaapiDevice,
            QsvExtraHardwareFrames,
            Resolution.Id,
            ScalingBehavior,
            VideoFormat,
            VideoProfile,
            VideoPreset,
            AllowBFrames,
            BitDepth,
            VideoBitrate,
            VideoBufferSize,
            TonemapAlgorithm,
            AudioFormat,
            AudioBitrate,
            AudioBufferSize,
            NormalizeLoudnessMode,
            AudioChannels,
            AudioSampleRate,
            NormalizeFramerate,
            DeinterlaceVideo
        );
}
