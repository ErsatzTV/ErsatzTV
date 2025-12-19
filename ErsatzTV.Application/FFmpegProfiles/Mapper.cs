using ErsatzTV.Core.Api.FFmpegProfiles;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.FFmpegProfiles;

internal static class Mapper
{
    internal static FFmpegProfileViewModel ProjectToViewModel(FFmpegProfile profile) =>
        new(
            profile.Id,
            profile.Name,
            profile.ThreadCount,
            profile.HardwareAcceleration,
            profile.VaapiDisplay ?? "drm",
            profile.VaapiDriver,
            profile.VaapiDevice,
            profile.QsvExtraHardwareFrames,
            Resolutions.Mapper.ProjectToViewModel(profile.Resolution),
            profile.ScalingBehavior,
            profile.VideoFormat,
            profile.VideoProfile,
            profile.VideoPreset ?? string.Empty,
            profile.AllowBFrames,
            profile.BitDepth,
            profile.VideoBitrate,
            profile.VideoBufferSize,
            profile.TonemapAlgorithm,
            profile.AudioFormat,
            profile.AudioBitrate,
            profile.AudioBufferSize,
            profile.NormalizeLoudnessMode,
            profile.TargetLoudness,
            profile.AudioChannels,
            profile.AudioSampleRate,
            profile.NormalizeFramerate,
            profile.DeinterlaceVideo == true);

    internal static FFmpegProfileResponseModel ProjectToResponseModel(FFmpegProfile ffmpegProfile) =>
        new(
            ffmpegProfile.Id,
            ffmpegProfile.Name,
            $"{ffmpegProfile.Resolution.Width}x{ffmpegProfile.Resolution.Height}",
            ffmpegProfile.VideoFormat.ToString().ToLowerInvariant(),
            ffmpegProfile.AudioFormat.ToString().ToLowerInvariant());

    internal static FFmpegFullProfileResponseModel ProjectToFullResponseModel(FFmpegProfile ffmpegProfile) =>
        new(
            ffmpegProfile.Id,
            ffmpegProfile.Name,
            ffmpegProfile.ThreadCount,
            ffmpegProfile.HardwareAcceleration,
            ffmpegProfile.VaapiDisplay,
            ffmpegProfile.VaapiDriver,
            ffmpegProfile.VaapiDevice,
            ffmpegProfile.QsvExtraHardwareFrames,
            ffmpegProfile.Resolution.Name,
            ffmpegProfile.ScalingBehavior,
            ffmpegProfile.VideoFormat,
            ffmpegProfile.VideoProfile,
            ffmpegProfile.VideoPreset,
            ffmpegProfile.AllowBFrames,
            ffmpegProfile.BitDepth,
            ffmpegProfile.VideoBitrate,
            ffmpegProfile.VideoBufferSize,
            ffmpegProfile.TonemapAlgorithm,
            ffmpegProfile.AudioFormat,
            ffmpegProfile.AudioBitrate,
            ffmpegProfile.AudioBufferSize,
            ffmpegProfile.NormalizeLoudnessMode,
            ffmpegProfile.AudioChannels,
            ffmpegProfile.AudioSampleRate,
            ffmpegProfile.NormalizeFramerate,
            ffmpegProfile.DeinterlaceVideo);
}
