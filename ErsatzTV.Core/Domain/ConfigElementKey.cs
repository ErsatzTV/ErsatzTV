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
    }
}
