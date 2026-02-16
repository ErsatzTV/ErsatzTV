namespace ErsatzTV.Core;

public class SystemEnvironment
{
    static SystemEnvironment()
    {
        BaseUrl = Environment.GetEnvironmentVariable("ETV_BASE_URL");

        ConfigFolder = Environment.GetEnvironmentVariable("ETV_CONFIG_FOLDER");

        TranscodeFolder = Environment.GetEnvironmentVariable("ETV_TRANSCODE_FOLDER");

        InstanceID = Environment.GetEnvironmentVariable("ETV_INSTANCE_ID") ?? "ersatztv.org";

        string uiPortVariable = Environment.GetEnvironmentVariable("ETV_UI_PORT");
        if (!int.TryParse(uiPortVariable, out int uiPort))
        {
            uiPort = 8409;
        }

        UiPort = uiPort;

        string streamingPortVariable = Environment.GetEnvironmentVariable("ETV_STREAMING_PORT");
        if (!int.TryParse(streamingPortVariable, out int streamingPort))
        {
            streamingPort = 8409;
        }

        StreamingPort = streamingPort;

        AllowSharedPlexServers =
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ETV_ALLOW_SHARED_PLEX_SERVERS"));

        string maximumUploadMbVariable = Environment.GetEnvironmentVariable("ETV_MAXIMUM_UPLOAD_MB");
        if (!int.TryParse(maximumUploadMbVariable, out int maximumUploadMb))
        {
            maximumUploadMb = 10;
        }

        MaximumUploadMb = maximumUploadMb;

        string slowDbMsVariable = Environment.GetEnvironmentVariable("ETV_SLOW_DB_MS");
        if (int.TryParse(slowDbMsVariable, out int slowDbMs) && slowDbMs > 0)
        {
            SlowDbMs = slowDbMs;
        }

        string slowApiMsVariable = Environment.GetEnvironmentVariable("ETV_SLOW_API_MS");
        if (int.TryParse(slowApiMsVariable, out int slowApiMs) && slowApiMs > 0)
        {
            SlowApiMs = slowApiMs;
        }

        string jellyfinPageSizeVariable = Environment.GetEnvironmentVariable("ETV_JF_PAGE_SIZE");
        if (!int.TryParse(jellyfinPageSizeVariable, out int jellyfinPageSize) || jellyfinPageSize <= 0)
        {
            jellyfinPageSize = 10;
        }

        JellyfinPageSize = jellyfinPageSize;

        JellyfinEnableStats = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ETV_JF_ENABLE_STATS"));
    }

    public static string BaseUrl { get; }
    public static string ConfigFolder { get; }
    public static string TranscodeFolder { get; }
    public static int UiPort { get; }
    public static int StreamingPort { get; }
    public static bool AllowSharedPlexServers { get; }
    public static int MaximumUploadMb { get; }
    public static int? SlowDbMs { get; }
    public static int? SlowApiMs { get; }
    public static int JellyfinPageSize { get; }
    public static bool JellyfinEnableStats { get; }
    public static string InstanceID { get; }
}
