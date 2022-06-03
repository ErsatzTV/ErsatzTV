using System.Text;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Core.FFmpeg;

public class MusicVideoCreditsGenerator : IMusicVideoCreditsGenerator
{
    private readonly ITempFilePool _tempFilePool;

    public MusicVideoCreditsGenerator(ITempFilePool tempFilePool) => _tempFilePool = tempFilePool;

    public async Task<Option<Subtitle>> GenerateCreditsSubtitle(MusicVideo musicVideo, FFmpegProfile ffmpegProfile)
    {
        const int HORIZONTAL_MARGIN_PERCENT = 3;
        const int VERTICAL_MARGIN_PERCENT = 5;

        var fontSize = (int)Math.Round(ffmpegProfile.Resolution.Height / 20.0);

        int leftMarginPercent = HORIZONTAL_MARGIN_PERCENT;
        int rightMarginPercent = HORIZONTAL_MARGIN_PERCENT;

        var leftMargin = (int)Math.Round(leftMarginPercent / 100.0 * ffmpegProfile.Resolution.Width);
        var rightMargin = (int)Math.Round(rightMarginPercent / 100.0 * ffmpegProfile.Resolution.Width);
        var verticalMargin =
            (int)Math.Round(VERTICAL_MARGIN_PERCENT / 100.0 * ffmpegProfile.Resolution.Height);

        foreach (MusicVideoMetadata metadata in musicVideo.MusicVideoMetadata)
        {
            var sb = new StringBuilder();

            string artist = string.Empty;
            foreach (ArtistMetadata artistMetadata in Optional(metadata.MusicVideo?.Artist?.ArtistMetadata).Flatten())
            {
                artist = artistMetadata.Title;
            }

            if (!string.IsNullOrWhiteSpace(artist))
            {
                sb.Append(artist);
            }

            if (!string.IsNullOrWhiteSpace(metadata.Title))
            {
                sb.Append($"\\N\"{metadata.Title}\"");
            }

            if (!string.IsNullOrWhiteSpace(metadata.Album))
            {
                sb.Append($"\\N{metadata.Album}");
            }

            string subtitles = await new SubtitleBuilder(_tempFilePool)
                .WithResolution(ffmpegProfile.Resolution)
                .WithFontName("OPTIKabel-Heavy")
                .WithFontSize(fontSize)
                .WithPrimaryColor("&HFFFFFF")
                .WithOutlineColor("&H444444")
                .WithAlignment(0)
                .WithMarginRight(rightMargin)
                .WithMarginLeft(leftMargin)
                .WithMarginV(verticalMargin)
                .WithBorderStyle(1)
                .WithShadow(3)
                .WithFormattedContent(sb.ToString())
                .WithStartEnd(TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(16))
                .WithFade(true)
                .BuildFile();

            return new Subtitle
            {
                Codec = "ass",
                Default = true,
                Forced = true,
                IsExtracted = false,
                SubtitleKind = SubtitleKind.Generated,
                Path = subtitles,
                SDH = false
            };
        }

        return None;
    }
}
