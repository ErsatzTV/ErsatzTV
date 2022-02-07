namespace ErsatzTV.Application.FFmpegProfiles
{
    public class FFmpegSettingsViewModel
    {
        public string FFmpegPath { get; set; }
        public string FFprobePath { get; set; }
        public int DefaultFFmpegProfileId { get; set; }
        public string PreferredLanguageCode { get; set; }
        public bool SaveReports { get; set; }
        public int? GlobalWatermarkId { get; set; }
        public int? GlobalFallbackFillerId { get; set; }
        public int HlsSegmenterIdleTimeout { get; set; }
        public int WorkAheadSegmenterLimit { get; set; }
        public int InitialSegmentCount { get; set; }
    }
}
