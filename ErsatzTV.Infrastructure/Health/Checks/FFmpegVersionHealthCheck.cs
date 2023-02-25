using System.Text.RegularExpressions;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Infrastructure.Health.Checks;

public class FFmpegVersionHealthCheck : BaseHealthCheck, IFFmpegVersionHealthCheck
{
    private const string BundledVersion = "5.1";
    private const string BundledVersionVaapi = "N-109960-g4f9d38e65d";
    private readonly IConfigElementRepository _configElementRepository;

    public FFmpegVersionHealthCheck(IConfigElementRepository configElementRepository) =>
        _configElementRepository = configElementRepository;

    public override string Title => "FFmpeg Version";

    public async Task<HealthCheckResult> Check(CancellationToken cancellationToken)
    {
        Option<ConfigElement> maybeFFmpegPath = await _configElementRepository.Get(ConfigElementKey.FFmpegPath);
        if (maybeFFmpegPath.IsNone)
        {
            return FailResult("Unable to locate ffmpeg");
        }

        Option<ConfigElement> maybeFFprobePath = await _configElementRepository.Get(ConfigElementKey.FFprobePath);
        if (maybeFFprobePath.IsNone)
        {
            return FailResult("Unable to locate ffprobe");
        }

        foreach (ConfigElement ffmpegPath in maybeFFmpegPath)
        {
            Option<string> maybeVersion = await GetVersion(ffmpegPath.Value, cancellationToken);
            if (maybeVersion.IsNone)
            {
                return WarningResult("Unable to determine ffmpeg version");
            }

            foreach (string version in maybeVersion)
            {
                foreach (HealthCheckResult result in ValidateVersion(version, "ffmpeg"))
                {
                    return result;
                }
            }
        }

        foreach (ConfigElement ffprobePath in maybeFFprobePath)
        {
            Option<string> maybeVersion = await GetVersion(ffprobePath.Value, cancellationToken);
            if (maybeVersion.IsNone)
            {
                return WarningResult("Unable to determine ffprobe version");
            }

            foreach (string version in maybeVersion)
            {
                foreach (HealthCheckResult result in ValidateVersion(version, "ffprobe"))
                {
                    return result;
                }
            }
        }

        return new HealthCheckResult("FFmpeg Version", HealthCheckStatus.Pass, string.Empty, None);
    }

    private Option<HealthCheckResult> ValidateVersion(string version, string app)
    {
        if (version.StartsWith("3.") || version.StartsWith("4."))
        {
            return FailResult($"{app} version {version} is too old; please install 5.1!");
        }

        if (!version.StartsWith("5.1") && version != BundledVersion && version != BundledVersionVaapi)
        {
            return WarningResult(
                $"{app} version {version} is unexpected and may have problems; please install 5.1!");
        }

        return None;
    }

    private static async Task<Option<string>> GetVersion(string path, CancellationToken cancellationToken)
    {
        Option<string> maybeLine = await GetProcessOutput(path, new[] { "-version" }, cancellationToken)
            .Map(s => s.Split("\n").HeadOrNone().Map(h => h.Trim()));
        foreach (string line in maybeLine)
        {
            const string PATTERN = @"version\s+([^\s]+)";
            Match match = Regex.Match(line, PATTERN);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return None;
    }
}
