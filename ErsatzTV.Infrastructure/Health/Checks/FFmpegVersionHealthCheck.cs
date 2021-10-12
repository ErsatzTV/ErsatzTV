using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Health.Checks
{
    public class FFmpegVersionHealthCheck : BaseHealthCheck, IFFmpegVersionHealthCheck
    {
        private const string BundledVersion = "N-104321-ga742ba60f1";
        private readonly IConfigElementRepository _configElementRepository;

        public FFmpegVersionHealthCheck(IConfigElementRepository configElementRepository)
        {
            _configElementRepository = configElementRepository;
        }

        public async Task<HealthCheckResult> Check()
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
                Option<string> maybeVersion = await GetVersion(ffmpegPath.Value);
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
                Option<string> maybeVersion = await GetVersion(ffprobePath.Value);
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

            return new HealthCheckResult("FFmpeg Version", HealthCheckStatus.Pass, string.Empty);
        }
        
        private Option<HealthCheckResult> ValidateVersion(string version, string app)
        {
            if (version.StartsWith("3."))
            {
                return FailResult($"{app} version {version} is too old; please install 4.4!");
            }

            if (version.StartsWith("4.3"))
            {
                return WarningResult($"{app} version 4.4 is now supported, please upgrade from 4.3!");
            }

            if (!version.StartsWith("4.4") && version != BundledVersion)
            {
                return WarningResult($"{app} version {version} is unexpected and may have problems; please install 4.4!");
            }

            return None;
        }

        private static async Task<Option<string>> GetVersion(string path)
        {
            Option<string> maybeLine = await GetProcessOutput(path, new[] { "-version" })
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

        protected override string Title => "FFmpeg Version";
    }
}
