﻿using System.Text.RegularExpressions;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Infrastructure.Health.Checks;

public class FFmpegVersionHealthCheck : BaseHealthCheck, IFFmpegVersionHealthCheck
{
    private const string BundledVersion = "7.0";
    private const string BundledVersionVaapi = "6.1";
    private const string WindowsVersionPrefix = "n6.1";

    private static readonly string[] FFmpegVersionArguments = { "-version" };

    private readonly IConfigElementRepository _configElementRepository;

    public FFmpegVersionHealthCheck(IConfigElementRepository configElementRepository) =>
        _configElementRepository = configElementRepository;

    public override string Title => "FFmpeg Version";

    public async Task<HealthCheckResult> Check(CancellationToken cancellationToken)
    {
        Option<ConfigElement> maybeFFmpegPath =
            await _configElementRepository.GetConfigElement(ConfigElementKey.FFmpegPath);
        if (maybeFFmpegPath.IsNone)
        {
            return FailResult("Unable to locate ffmpeg");
        }

        Option<ConfigElement> maybeFFprobePath =
            await _configElementRepository.GetConfigElement(ConfigElementKey.FFprobePath);
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
        if (version.StartsWith("3.", StringComparison.OrdinalIgnoreCase) ||
            version.StartsWith("4.", StringComparison.OrdinalIgnoreCase) ||
            version.StartsWith("5.", StringComparison.OrdinalIgnoreCase))
        {
            return FailResult($"{app} version {version} is too old; please install 6.1!");
        }

        if (!version.StartsWith("6.1", StringComparison.OrdinalIgnoreCase) &&
            !version.StartsWith(WindowsVersionPrefix, StringComparison.OrdinalIgnoreCase) &&
            version != BundledVersion &&
            version != BundledVersionVaapi)
        {
            return WarningResult(
                $"{app} version {version} is unexpected and may have problems; please install 6.1!");
        }

        return None;
    }

    private static async Task<Option<string>> GetVersion(string path, CancellationToken cancellationToken)
    {
        Option<string> maybeLine = await GetProcessOutput(path, FFmpegVersionArguments, cancellationToken)
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
