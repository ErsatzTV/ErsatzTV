using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Infrastructure.Health.Checks;

public class FFmpegReportsHealthCheck : BaseHealthCheck, IFFmpegReportsHealthCheck
{
    private readonly IConfigElementRepository _configElementRepository;

    public FFmpegReportsHealthCheck(IConfigElementRepository configElementRepository) =>
        _configElementRepository = configElementRepository;

    protected override string Title => "FFmpeg Reports";

    public async Task<HealthCheckResult> Check(CancellationToken cancellationToken)
    {
        Option<bool> saveReports =
            await _configElementRepository.GetValue<bool>(ConfigElementKey.FFmpegSaveReports);

        foreach (bool value in saveReports)
        {
            if (value)
            {
                return Result(
                    HealthCheckStatus.Warning,
                    "FFmpeg troubleshooting reports are enabled and may use a lot of disk space");
            }
        }

        return OkResult();
    }
}
