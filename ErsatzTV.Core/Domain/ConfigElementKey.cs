namespace ErsatzTV.Core.Domain
{
    public class ConfigElementKey
    {
        private ConfigElementKey(string key) => Key = key;

        public string Key { get; }

        public static ConfigElementKey FFmpegPath => new("ffmpeg.ffmpeg_path");
        public static ConfigElementKey FFprobePath => new("ffmpeg.ffprobe_path");
        public static ConfigElementKey FFmpegDefaultProfileId => new("ffmpeg.default_profile_id");
        public static ConfigElementKey FFmpegDefaultResolutionId => new("ffmpeg.default_resolution_id");
        public static ConfigElementKey FFmpegSaveReports => new("ffmpeg.save_reports");
        public static ConfigElementKey FFmpegPreferredLanguageCode => new("ffmpeg.preferred_language_code");
        public static ConfigElementKey FFmpegGlobalWatermarkId => new("ffmpeg.global_watermark_id");
        public static ConfigElementKey FFmpegVaapiDriver => new("ffmpeg.vaapi_driver");
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
        public static ConfigElementKey LogsPageSize => new("pages.logs.page_size");
        public static ConfigElementKey LibraryRefreshInterval => new("scanner.library_refresh_interval");
        public static ConfigElementKey PlayoutDaysToBuild => new("playout.days_to_build");
    }
}
