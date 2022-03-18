using ErsatzTV.Application.FFmpegProfiles;
using ErsatzTV.Application.Resolutions;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.ViewModels;

public class FFmpegProfileEditViewModel
{
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
        NormalizeLoudness = viewModel.NormalizeLoudness;
        Id = viewModel.Id;
        Name = viewModel.Name;
        NormalizeAudio = viewModel.NormalizeAudio;
        NormalizeVideo = viewModel.NormalizeVideo;
        NormalizeFramerate = viewModel.NormalizeFramerate;
        DeinterlaceVideo = viewModel.DeinterlaceVideo;
        Resolution = viewModel.Resolution;
        ThreadCount = viewModel.ThreadCount;
        Transcode = viewModel.Transcode;
        HardwareAcceleration = viewModel.HardwareAcceleration;
        VaapiDriver = viewModel.VaapiDriver;
        VaapiDevice = viewModel.VaapiDevice;
        VideoBitrate = viewModel.VideoBitrate;
        VideoBufferSize = viewModel.VideoBufferSize;
        VideoFormat = viewModel.VideoFormat;
    }

    public int AudioBitrate { get; set; }
    public int AudioBufferSize { get; set; }
    public int AudioChannels { get; set; }
    public FFmpegProfileAudioFormat AudioFormat { get; set; }
    public int AudioSampleRate { get; set; }
    public bool NormalizeLoudness { get; set; }
    public int Id { get; set; }
    public string Name { get; set; }
    public bool NormalizeAudio { get; set; }
    public bool NormalizeVideo { get; set; }
    public bool NormalizeFramerate { get; set; }
    public bool DeinterlaceVideo { get; set; }
    public ResolutionViewModel Resolution { get; set; }
    public int ThreadCount { get; set; }
    public bool Transcode { get; set; }
    public HardwareAccelerationKind HardwareAcceleration { get; set; }
    public VaapiDriver VaapiDriver { get; set; }
    public string VaapiDevice { get; set; }
    public int VideoBitrate { get; set; }
    public int VideoBufferSize { get; set; }
    public FFmpegProfileVideoFormat VideoFormat { get; set; }

    public CreateFFmpegProfile ToCreate() =>
        new(
            Name,
            ThreadCount,
            Transcode,
            HardwareAcceleration,
            VaapiDriver,
            VaapiDevice,
            Resolution.Id,
            NormalizeVideo,
            VideoFormat,
            VideoBitrate,
            VideoBufferSize,
            AudioFormat,
            AudioBitrate,
            AudioBufferSize,
            NormalizeLoudness,
            AudioChannels,
            AudioSampleRate,
            NormalizeAudio,
            NormalizeFramerate,
            DeinterlaceVideo
        );

    public UpdateFFmpegProfile ToUpdate() =>
        new(
            Id,
            Name,
            ThreadCount,
            Transcode,
            HardwareAcceleration,
            VaapiDriver,
            VaapiDevice,
            Resolution.Id,
            NormalizeVideo,
            VideoFormat,
            VideoBitrate,
            VideoBufferSize,
            AudioFormat,
            AudioBitrate,
            AudioBufferSize,
            NormalizeLoudness,
            AudioChannels,
            AudioSampleRate,
            NormalizeAudio,
            NormalizeFramerate,
            DeinterlaceVideo
        );
}