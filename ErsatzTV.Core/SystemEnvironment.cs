namespace ErsatzTV.Core;

public class SystemEnvironment
{
    static SystemEnvironment()
    {
        BaseUrl = Environment.GetEnvironmentVariable("ETV_BASE_URL");

        ConfigFolder = Environment.GetEnvironmentVariable("ETV_CONFIG_FOLDER");

        TranscodeFolder = Environment.GetEnvironmentVariable("ETV_TRANSCODE_FOLDER");

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

        string slowQueryMsVariable = Environment.GetEnvironmentVariable("ETV_SLOW_QUERY_MS");
        if (int.TryParse(slowQueryMsVariable, out int slowQueryMs))
        {
            SlowQueryMs = slowQueryMs;
        }
    }

    public static string BaseUrl { get; }
    public static string ConfigFolder { get; }
    public static string TranscodeFolder { get; }
    public static int UiPort { get; }
    public static int StreamingPort { get; }
    public static bool AllowSharedPlexServers { get; }
    public static int MaximumUploadMb { get; }
    public static int? SlowQueryMs { get; }
}
