﻿using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.Application.FFmpegProfiles;

public record CreateFFmpegProfile(
    string Name,
    int ThreadCount,
    HardwareAccelerationKind HardwareAcceleration,
    VaapiDriver VaapiDriver,
    string VaapiDevice,
    int? QsvExtraHardwareFrames,
    int ResolutionId,
    ScalingBehavior ScalingBehavior,
    FFmpegProfileVideoFormat VideoFormat,
    string VideoProfile,
    string VideoPreset,
    FFmpegProfileBitDepth BitDepth,
    int VideoBitrate,
    int VideoBufferSize,
    FFmpegProfileAudioFormat AudioFormat,
    int AudioBitrate,
    int AudioBufferSize,
    NormalizeLoudnessMode NormalizeLoudnessMode,
    int AudioChannels,
    int AudioSampleRate,
    bool NormalizeFramerate,
    bool DeinterlaceVideo) : IRequest<Either<BaseError, CreateFFmpegProfileResult>>;
