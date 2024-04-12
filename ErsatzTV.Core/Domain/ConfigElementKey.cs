﻿namespace ErsatzTV.Core.Domain;

public class ConfigElementKey
{
    private ConfigElementKey(string key) => Key = key;

    public string Key { get; }

    public static ConfigElementKey MinimumLogLevel => new("log.minimum_level");
    public static ConfigElementKey MinimumLogLevelScanning => new("log.minimum_level.scanning");
    public static ConfigElementKey MinimumLogLevelScheduling => new("log.minimum_level.scheduling");
    public static ConfigElementKey MinimumLogLevelStreaming => new("log.minimum_level.streaming");
    public static ConfigElementKey MinimumLogLevelHttp => new("log.minimum_level.http");
    public static ConfigElementKey FFmpegPath => new("ffmpeg.ffmpeg_path");
    public static ConfigElementKey FFprobePath => new("ffmpeg.ffprobe_path");
    public static ConfigElementKey FFmpegDefaultProfileId => new("ffmpeg.default_profile_id");
    public static ConfigElementKey FFmpegDefaultResolutionId => new("ffmpeg.default_resolution_id");
    public static ConfigElementKey FFmpegSaveReports => new("ffmpeg.save_reports");
    public static ConfigElementKey FFmpegUseEmbeddedSubtitles => new("ffmpeg.use_embedded_subtitles");
    public static ConfigElementKey FFmpegExtractEmbeddedSubtitles => new("ffmpeg.extract_embedded_subtitles");
    public static ConfigElementKey FFmpegPreferredLanguageCode => new("ffmpeg.preferred_language_code");
    public static ConfigElementKey FFmpegGlobalWatermarkId => new("ffmpeg.global_watermark_id");
    public static ConfigElementKey FFmpegGlobalFallbackFillerId => new("ffmpeg.global_fallback_filler_id");
    public static ConfigElementKey FFmpegSegmenterTimeout => new("ffmpeg.segmenter.timeout_seconds");
    public static ConfigElementKey FFmpegWorkAheadSegmenters => new("ffmpeg.segmenter.work_ahead_limit");
    public static ConfigElementKey FFmpegInitialSegmentCount => new("ffmpeg.segmenter.initial_segment_count");
    public static ConfigElementKey FFmpegHlsDirectOutputFormat => new("ffmpeg.hls_direct.output_format");
    public static ConfigElementKey SearchIndexVersion => new("search_index.version");
    public static ConfigElementKey HDHRTunerCount => new("hdhr.tuner_count");
    public static ConfigElementKey ChannelsPageSize => new("pages.channels.page_size");
    public static ConfigElementKey CollectionsPageSize => new("pages.collections.page_size");
    public static ConfigElementKey MultiCollectionsPageSize => new("pages.multi_collections.page_size");
    public static ConfigElementKey SmartCollectionsPageSize => new("pages.smart_collections.page_size");
    public static ConfigElementKey SchedulesPageSize => new("pages.schedules.page_size");
    public static ConfigElementKey SchedulesDetailPageSize => new("pages.schedules.detail_page_size");
    public static ConfigElementKey PlayoutsPageSize => new("pages.playouts.page_size");
    public static ConfigElementKey PlayoutsDetailPageSize => new("pages.playouts.detail_page_size");
    public static ConfigElementKey PlayoutsDetailShowFiller => new("pages.playouts.detail_show_filler");
    public static ConfigElementKey LogsPageSize => new("pages.logs.page_size");
    public static ConfigElementKey TraktListsPageSize => new("pages.trakt.lists_page_size");
    public static ConfigElementKey FillerPresetsPageSize => new("pages.filler_presets.page_size");
    public static ConfigElementKey LibraryRefreshInterval => new("scanner.library_refresh_interval");
    public static ConfigElementKey PlayoutDaysToBuild => new("playout.days_to_build");
    public static ConfigElementKey PlayoutSkipMissingItems => new("playout.skip_missing_items");
    public static ConfigElementKey XmltvTimeZone => new("xmltv.time_zone");
    public static ConfigElementKey XmltvDaysToBuild => new("xmltv.days_to_build");
}
