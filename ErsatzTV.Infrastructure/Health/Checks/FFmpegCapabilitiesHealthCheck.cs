using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.FFmpeg.Capabilities;

namespace ErsatzTV.Infrastructure.Health.Checks;

public class FFmpegCapabilitiesHealthCheck(IConfigElementRepository configElementRepository, IHardwareCapabilitiesFactory hardwareCapabilitiesFactory)
    : BaseHealthCheck, IFFmpegCapabilitiesHealthCheck
{
    public override string Title => "FFmpeg Capabilities";

    public async Task<HealthCheckResult> Check(CancellationToken cancellationToken)
    {
        Option<ConfigElement> maybeFFmpegPath =
            await configElementRepository.GetConfigElement(ConfigElementKey.FFmpegPath, cancellationToken);
        if (maybeFFmpegPath.IsNone)
        {
            // ffmpeg version health check will surface this
            return NotApplicableResult();
        }

        foreach (ConfigElement ffmpegPath in maybeFFmpegPath)
        {
            var ffmpegCapabilities =
                await hardwareCapabilitiesFactory.GetFFmpegCapabilities(ffmpegPath.Value, cancellationToken);

            List<string> missingFilters = [];

            foreach (FFmpegKnownFilter filter in FFmpegKnownFilter.RequiredFilters)
            {
                if (!ffmpegCapabilities.HasFilter(filter))
                {
                    missingFilters.Add(filter.Name);
                }
            }

            if (missingFilters.Count > 0)
            {
                return FailResult(
                    $"FFmpeg is missing required filters and will NOT work correctly: [{string.Join(", ", missingFilters)}]",
                    "FFmpeg is missing required filters and will NOT work correctly");
            }
        }

        return NotApplicableResult();
    }
}
