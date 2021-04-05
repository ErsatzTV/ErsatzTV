using ErsatzTV.Application.Resolutions;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.FFmpegProfiles
{
    internal static class Mapper
    {
        internal static FFmpegProfileViewModel ProjectToViewModel(FFmpegProfile profile) =>
            new(
                profile.Id,
                profile.Name,
                profile.ThreadCount,
                profile.Transcode,
                profile.HardwareAcceleration,
                Project(profile.Resolution),
                profile.NormalizeVideo,
                profile.VideoCodec,
                profile.VideoBitrate,
                profile.VideoBufferSize,
                profile.AudioCodec,
                profile.AudioBitrate,
                profile.AudioBufferSize,
                profile.NormalizeLoudness,
                profile.AudioChannels,
                profile.AudioSampleRate,
                profile.NormalizeAudio,
                profile.FrameRate);

        private static ResolutionViewModel Project(Resolution resolution) =>
            new(resolution.Id, resolution.Name, resolution.Width, resolution.Height);
    }
}
