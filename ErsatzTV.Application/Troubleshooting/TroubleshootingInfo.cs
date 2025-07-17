using ErsatzTV.Application.FFmpegProfiles;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Troubleshooting;

public record TroubleshootingInfo(
    string Version,
    Dictionary<string, string> Environment,
    List<HealthCheckResultSummary> Health,
    FFmpegSettingsViewModel FFmpegSettings,
    List<FFmpegProfile> FFmpegProfiles,
    List<Channel> Channels,
    List<ChannelWatermark> Watermarks,
    string NvidiaCapabilities,
    string QsvCapabilities,
    string VaapiCapabilities);
