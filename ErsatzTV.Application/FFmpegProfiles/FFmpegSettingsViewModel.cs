using ErsatzTV.FFmpeg.OutputFormat;

namespace ErsatzTV.Application.FFmpegProfiles;

public class FFmpegSettingsViewModel
{
    public string FFmpegPath { get; set; }
    public string FFprobePath { get; set; }
    public int DefaultFFmpegProfileId { get; set; }
    public string PreferredAudioLanguageCode { get; set; }
    public bool UseEmbeddedSubtitles { get; set; }
    public bool ExtractEmbeddedSubtitles { get; set; }
    public bool SaveReports { get; set; }
    public int? GlobalWatermarkId { get; set; }
    public int? GlobalFallbackFillerId { get; set; }
    public int HlsSegmenterIdleTimeout { get; set; }
    public int WorkAheadSegmenterLimit { get; set; }
    public int InitialSegmentCount { get; set; }
    public OutputFormatKind HlsDirectOutputFormat { get; set; }
    public string DefaultMpegTsScript { get; set; }
}
