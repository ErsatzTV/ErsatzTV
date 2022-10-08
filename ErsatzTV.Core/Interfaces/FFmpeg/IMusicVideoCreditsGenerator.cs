using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.FFmpeg;

public interface IMusicVideoCreditsGenerator
{
    Task<Option<Subtitle>> GenerateCreditsSubtitle(MusicVideo musicVideo, FFmpegProfile ffmpegProfile);

    Task<Option<Subtitle>> GenerateCreditsSubtitleFromTemplate(
        MusicVideo musicVideo,
        FFmpegProfile ffmpegProfile,
        string templateFileName);
}
