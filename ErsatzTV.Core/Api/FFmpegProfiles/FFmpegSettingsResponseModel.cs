namespace ErsatzTV.Core.Api.FFmpegProfiles;

public record FFmpegProfileSettingsResponseModel(
    string FFmpegPath,
    string FFprobePath,
    int DefaultFFmpegProfileId,
    string PreferredAudioLanguageCode,
    bool SaveReports,
    int? GlobalWatermarkId,
    int? GlobalFallbackFillerId,
    int HlsSegmenterIdleTimeout,
    int WorkAheadSegmenterLimit,
    int InitialSegmentCount
);
