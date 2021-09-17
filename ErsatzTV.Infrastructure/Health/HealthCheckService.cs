using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using LanguageExt;

namespace ErsatzTV.Infrastructure.Health
{
    public class HealthCheckService : IHealthCheckService
    {
        private readonly List<IHealthCheck> _checks;

        // ReSharper disable SuggestBaseTypeForParameterInConstructor
        public HealthCheckService(
            IFFmpegVersionHealthCheck ffmpegVersionHealthCheck,
            IFFmpegReportsHealthCheck fFmpegReportsHealthCheck,
            IHardwareAccelerationHealthCheck hardwareAccelerationHealthCheck,
            IMovieMetadataHealthCheck movieMetadataHealthCheck,
            IEpisodeMetadataHealthCheck episodeMetadataHealthCheck,
            IZeroDurationHealthCheck zeroDurationHealthCheck)
        {
            _checks = new List<IHealthCheck>
            {
                ffmpegVersionHealthCheck,
                fFmpegReportsHealthCheck,
                hardwareAccelerationHealthCheck,
                movieMetadataHealthCheck,
                episodeMetadataHealthCheck,
                zeroDurationHealthCheck
            };
        }

        public Task<List<HealthCheckResult>> PerformHealthChecks() =>
            _checks.Map(c => c.Check()).Sequence().Map(results => results.ToList());
    }
}
