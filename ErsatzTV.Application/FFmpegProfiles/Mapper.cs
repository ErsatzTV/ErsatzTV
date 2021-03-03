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
                profile.NormalizeResolution,
                profile.VideoCodec,
                profile.NormalizeVideoCodec,
                profile.VideoBitrate,
                profile.VideoBufferSize,
                profile.AudioCodec,
                profile.NormalizeAudioCodec,
                profile.AudioBitrate,
                profile.AudioBufferSize,
                profile.AudioVolume,
                profile.AudioChannels,
                profile.AudioSampleRate,
                profile.NormalizeAudio);

        private static ResolutionViewModel Project(Resolution resolution) =>
            new(resolution.Id, resolution.Name, resolution.Width, resolution.Height);
    }
}
