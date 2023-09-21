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
            profile.VaapiDriver,
            profile.VaapiDevice,
            profile.QsvExtraHardwareFrames,
            Resolutions.Mapper.ProjectToViewModel(profile.Resolution),
            profile.ScalingBehavior,
            profile.VideoFormat,
            profile.BitDepth,
            profile.VideoBitrate,
            profile.VideoBufferSize,
            profile.AudioFormat,
            profile.AudioBitrate,
            profile.AudioBufferSize,
            profile.NormalizeLoudness,
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
            (int)ffmpegProfile.HardwareAcceleration,
            (int)ffmpegProfile.VaapiDriver,
            ffmpegProfile.VaapiDevice,
            ffmpegProfile.ResolutionId,
            (int)ffmpegProfile.VideoFormat,
            ffmpegProfile.VideoBitrate,
            ffmpegProfile.VideoBufferSize,
            (int)ffmpegProfile.AudioFormat,
            ffmpegProfile.AudioBitrate,
            ffmpegProfile.AudioBufferSize,
            ffmpegProfile.NormalizeLoudness,
            ffmpegProfile.AudioChannels,
            ffmpegProfile.AudioSampleRate,
            ffmpegProfile.NormalizeFramerate,
            ffmpegProfile.DeinterlaceVideo);
}
