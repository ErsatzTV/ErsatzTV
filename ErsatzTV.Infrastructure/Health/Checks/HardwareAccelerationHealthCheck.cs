using System.Reflection;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Data;
using LanguageExt.UnsafeValueAccess;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Health.Checks;

public class HardwareAccelerationHealthCheck : BaseHealthCheck, IHardwareAccelerationHealthCheck
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public HardwareAccelerationHealthCheck(
        IDbContextFactory<TvContext> dbContextFactory,
        IConfigElementRepository configElementRepository)
    {
        _dbContextFactory = dbContextFactory;
        _configElementRepository = configElementRepository;
    }

    public override string Title => "Hardware Acceleration";

    public async Task<HealthCheckResult> Check(CancellationToken cancellationToken)
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
                accelerationKinds.Add(HardwareAccelerationKind.Qsv);
            }
        }

        if (!accelerationKinds.Any())
        {
            accelerationKinds.AddRange(
                await GetSupportedAccelerationKinds(maybeFFmpegPath.ValueUnsafe().Value, cancellationToken));
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
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

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

    private static async Task<List<HardwareAccelerationKind>> GetSupportedAccelerationKinds(
        string ffmpegPath,
        CancellationToken cancellationToken)
    {
        var result = new System.Collections.Generic.HashSet<HardwareAccelerationKind>();

        string output = await GetProcessOutput(ffmpegPath, new[] { "-v", "quiet", "-hwaccels" }, cancellationToken);
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
                case "cuda":
                    result.Add(HardwareAccelerationKind.Nvenc);
                    break;
                case "qsv":
                    result.Add(HardwareAccelerationKind.Qsv);
                    break;
                case "videotoolbox":
                    result.Add(HardwareAccelerationKind.VideoToolbox);
                    break;
            }
        }

        return result.ToList();
    }
}
