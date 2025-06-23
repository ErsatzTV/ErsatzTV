using ErsatzTV.Application.FFmpegProfiles;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Troubleshooting;

public record TroubleshootingInfo(
    string Version,
    Dictionary<string, string> Environment,
    IEnumerable<HealthCheckResultSummary> Health,
    FFmpegSettingsViewModel FFmpegSettings,
    IEnumerable<FFmpegProfile> FFmpegProfiles,
    IEnumerable<Channel> Channels,
    string NvidiaCapabilities,
    string QsvCapabilities,
    string VaapiCapabilities);
