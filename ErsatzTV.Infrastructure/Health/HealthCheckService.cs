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
            IZeroDurationHealthCheck zeroDurationHealthCheck,
            IVaapiDriverHealthCheck vaapiDriverHealthCheck)
        {
            _checks = new List<IHealthCheck>
            {
                ffmpegVersionHealthCheck,
                fFmpegReportsHealthCheck,
                hardwareAccelerationHealthCheck,
                movieMetadataHealthCheck,
                episodeMetadataHealthCheck,
                zeroDurationHealthCheck,
                vaapiDriverHealthCheck
            };
        }

        public Task<List<HealthCheckResult>> PerformHealthChecks() =>
            _checks.Map(c => c.Check()).SequenceParallel().Map(results => results.ToList());
    }
}
