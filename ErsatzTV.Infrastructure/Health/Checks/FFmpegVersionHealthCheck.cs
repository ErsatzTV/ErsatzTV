using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.FFmpeg.Capabilities;

namespace ErsatzTV.Infrastructure.Health.Checks;

public class FFmpegVersionHealthCheck(
    IConfigElementRepository configElementRepository,
    IHardwareCapabilitiesFactory hardwareCapabilitiesFactory)
    : BaseHealthCheck, IFFmpegVersionHealthCheck
{
    private const string BundledVersion = "7.1.1";
    private const string BundledVersionVaapi = "7.1.1";
    private const string WindowsVersionPrefix = "n7.1.1";

    public override string Title => "FFmpeg Version";

    public async Task<HealthCheckResult> Check(CancellationToken cancellationToken)
    {
        var link = new HealthCheckLink("https://github.com/ErsatzTV/ErsatzTV-ffmpeg/releases/tag/7.1.1");

        Option<ConfigElement> maybeFFmpegPath =
            await configElementRepository.GetConfigElement(ConfigElementKey.FFmpegPath, cancellationToken);
        if (maybeFFmpegPath.IsNone)
        {
            return FailResult("Unable to locate ffmpeg", "Unable to locate ffmpeg", link);
        }

        Option<ConfigElement> maybeFFprobePath =
            await configElementRepository.GetConfigElement(ConfigElementKey.FFprobePath, cancellationToken);
        if (maybeFFprobePath.IsNone)
        {
            return FailResult("Unable to locate ffprobe", "Unable to locate ffprobe", link);
        }

        foreach (ConfigElement ffmpegPath in maybeFFmpegPath)
        {
            Option<string> maybeVersion =
                await hardwareCapabilitiesFactory.GetFFmpegVersion(ffmpegPath.Value, cancellationToken);
            if (maybeVersion.IsNone || maybeVersion.Exists(string.IsNullOrWhiteSpace))
            {
                return WarningResult("Unable to determine ffmpeg version", "Unable to determine ffmpeg version", link);
            }

            foreach (string version in maybeVersion)
            {
                foreach (HealthCheckResult result in ValidateVersion(version, "ffmpeg", link))
                {
                    return result;
                }
            }
        }

        foreach (ConfigElement ffprobePath in maybeFFprobePath)
        {
            Option<string> maybeVersion =
                await hardwareCapabilitiesFactory.GetFFmpegVersion(ffprobePath.Value, cancellationToken);
            if (maybeVersion.IsNone || maybeVersion.Exists(string.IsNullOrWhiteSpace))
            {
                return WarningResult(
                    "Unable to determine ffprobe version",
                    "Unable to determine ffprobe version",
                    link);
            }

            foreach (string version in maybeVersion)
            {
                foreach (HealthCheckResult result in ValidateVersion(version, "ffprobe", link))
                {
                    return result;
                }
            }
        }

        return new HealthCheckResult("FFmpeg Version", HealthCheckStatus.Pass, string.Empty, string.Empty, None);
    }

    private Option<HealthCheckResult> ValidateVersion(string version, string app, HealthCheckLink link)
    {
        if (version.StartsWith("3.", StringComparison.OrdinalIgnoreCase) ||
            version.StartsWith("4.", StringComparison.OrdinalIgnoreCase) ||
            version.StartsWith("5.", StringComparison.OrdinalIgnoreCase) ||
            version.StartsWith("6.", StringComparison.OrdinalIgnoreCase))
        {
            return FailResult(
                $"{app} version {version} is too old; please install 7.1.1!",
                $"{app} version is too old",
                link);
        }

        if (!version.StartsWith("7.1.1", StringComparison.OrdinalIgnoreCase) &&
            !version.StartsWith(WindowsVersionPrefix, StringComparison.OrdinalIgnoreCase) &&
            version != BundledVersion &&
            version != BundledVersionVaapi)
        {
            return WarningResult(
                $"{app} version {version} is unexpected and may have problems; please install 7.1.1!",
                $"{app} version is unexpected",
                link);
        }

        return None;
    }
}
