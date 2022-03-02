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
            IFFmpegReportsHealthCheck ffmpegReportsHealthCheck,
            IHardwareAccelerationHealthCheck hardwareAccelerationHealthCheck,
            IMovieMetadataHealthCheck movieMetadataHealthCheck,
            IEpisodeMetadataHealthCheck episodeMetadataHealthCheck,
            IZeroDurationHealthCheck zeroDurationHealthCheck,
            IFileNotFoundHealthCheck fileNotFoundHealthCheck,
            IVaapiDriverHealthCheck vaapiDriverHealthCheck,
            IErrorReportsHealthCheck errorReportsHealthCheck)
        {
            _checks = new List<IHealthCheck>
            {
                ffmpegVersionHealthCheck,
                ffmpegReportsHealthCheck,
                hardwareAccelerationHealthCheck,
                movieMetadataHealthCheck,
                episodeMetadataHealthCheck,
                zeroDurationHealthCheck,
                fileNotFoundHealthCheck,
                vaapiDriverHealthCheck,
                errorReportsHealthCheck
            };
        }

        public Task<List<HealthCheckResult>> PerformHealthChecks() =>
            _checks.Map(c => c.Check()).SequenceParallel().Map(results => results.ToList());
    }
}
