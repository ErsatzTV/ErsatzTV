using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Data;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Health.Checks
{
    public class HardwareAccelerationHealthCheck : BaseHealthCheck, IHardwareAccelerationHealthCheck
    {
        private readonly IDbContextFactory<TvContext> _dbContextFactory;
        private readonly IConfigElementRepository _configElementRepository;

        public HardwareAccelerationHealthCheck(
            IDbContextFactory<TvContext> dbContextFactory,
            IConfigElementRepository configElementRepository)
        {
            _dbContextFactory = dbContextFactory;
            _configElementRepository = configElementRepository;
        }

        public async Task<HealthCheckResult> Check()
        {
            Option<ConfigElement> maybeFFmpegPath = await _configElementRepository.Get(ConfigElementKey.FFmpegPath);
            if (maybeFFmpegPath.IsNone)
            {
                return FailResult("Unable to locate ffmpeg");
            }

            string version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "unknown";

            var accelerationKinds = new List<HardwareAccelerationKind>();
            
            if (version.Contains("docker", StringComparison.OrdinalIgnoreCase))
            {
                if (version.Contains("nvidia", StringComparison.OrdinalIgnoreCase))
                {
                    accelerationKinds.Add(HardwareAccelerationKind.Nvenc);
                }
                else if (version.Contains("vaapi", StringComparison.OrdinalIgnoreCase))
                {
                    accelerationKinds.Add(HardwareAccelerationKind.Vaapi);
                }
            }

            if (!accelerationKinds.Any())
            {
                accelerationKinds.AddRange(await GetSupportedAccelerationKinds(maybeFFmpegPath.ValueUnsafe().Value));
            }

            if (!accelerationKinds.Any())
            {
                return InfoResult("No compatible hardware acceleration kinds are supported by ffmpeg");
            }

            Option<HealthCheckResult> maybeResult = await VerifyProfilesUseAcceleration(accelerationKinds);
            foreach (HealthCheckResult result in maybeResult)
            {
                return result;
            }

            return OkResult();
        }

        private async Task<Option<HealthCheckResult>> VerifyProfilesUseAcceleration(
            IEnumerable<HardwareAccelerationKind> accelerationKinds)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();

            List<Channel> badChannels = await dbContext.Channels
                .Filter(c => c.StreamingMode != StreamingMode.HttpLiveStreamingDirect)
                .Filter(c => !accelerationKinds.Contains(c.FFmpegProfile.HardwareAcceleration))
                .ToListAsync();

            if (badChannels.Any())
            {
                var accel = string.Join(", ", accelerationKinds);
                var channels = string.Join(", ", badChannels.Map(c => $"{c.Number} - {c.Name}"));
                return WarningResult(
                    $"The following channels are transcoding without hardware acceleration ({accel}): {channels}");
            }

            return None;
        }

        private static async Task<List<HardwareAccelerationKind>> GetSupportedAccelerationKinds(string ffmpegPath)
        {
            var result = new System.Collections.Generic.HashSet<HardwareAccelerationKind>();
            
            string output = await GetProcessOutput(ffmpegPath, new[] { "-v", "quiet", "-hwaccels" });
            foreach (string method in output.Split("\n").Map(s => s.Trim()).Skip(1))
            {
                switch (method)
                {
                    case "vaapi":
                        result.Add(HardwareAccelerationKind.Vaapi);
                        break;
                    case "nvenc":
                        result.Add(HardwareAccelerationKind.Nvenc);
                        break;
                    case "qsv":
                        // qsv is only supported on windows
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            result.Add(HardwareAccelerationKind.Qsv);
                        }
                        break;
                }
            }

            return result.ToList();
        }

        protected override string Title => "Hardware Acceleration";
    }
}
