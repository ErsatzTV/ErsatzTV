﻿using ErsatzTV.Application.FFmpegProfiles;
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
        NormalizeLoudnessMode = viewModel.NormalizeLoudnessMode;
        Id = viewModel.Id;
        Name = viewModel.Name;
        NormalizeFramerate = viewModel.NormalizeFramerate;
        DeinterlaceVideo = viewModel.DeinterlaceVideo;
        Resolution = viewModel.Resolution;
        ScalingBehavior = viewModel.ScalingBehavior;
        ThreadCount = viewModel.ThreadCount;
        HardwareAcceleration = viewModel.HardwareAcceleration;
        VaapiDriver = viewModel.VaapiDriver;
        VaapiDevice = viewModel.VaapiDevice;
        QsvExtraHardwareFrames = viewModel.QsvExtraHardwareFrames;
        VideoBitrate = viewModel.VideoBitrate;
        VideoBufferSize = viewModel.VideoBufferSize;
        VideoFormat = viewModel.VideoFormat;
        VideoProfile = viewModel.VideoProfile;
        VideoPreset = viewModel.VideoPreset;
        BitDepth = viewModel.BitDepth;
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
    public VaapiDriver VaapiDriver { get; set; }
    public string VaapiDevice { get; set; }
    public int? QsvExtraHardwareFrames { get; set; }
    public int VideoBitrate { get; set; }
    public int VideoBufferSize { get; set; }
    public FFmpegProfileVideoFormat VideoFormat { get; set; }
    public string VideoProfile { get; set; }
    public string VideoPreset { get; set; }
    public FFmpegProfileBitDepth BitDepth { get; set; }

    public CreateFFmpegProfile ToCreate() =>
        new(
            Name,
            ThreadCount,
            HardwareAcceleration,
            VaapiDriver,
            VaapiDevice,
            QsvExtraHardwareFrames,
            Resolution.Id,
            ScalingBehavior,
            VideoFormat,
            VideoProfile,
            VideoPreset,
            BitDepth,
            VideoBitrate,
            VideoBufferSize,
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
            VaapiDriver,
            VaapiDevice,
            QsvExtraHardwareFrames,
            Resolution.Id,
            ScalingBehavior,
            VideoFormat,
            VideoProfile,
            VideoPreset,
            BitDepth,
            VideoBitrate,
            VideoBufferSize,
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
