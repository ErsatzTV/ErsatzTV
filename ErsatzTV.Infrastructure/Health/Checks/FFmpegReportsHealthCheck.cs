using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Infrastructure.Health.Checks;

public class FFmpegReportsHealthCheck : BaseHealthCheck, IFFmpegReportsHealthCheck
{
    private readonly IConfigElementRepository _configElementRepository;

    public FFmpegReportsHealthCheck(IConfigElementRepository configElementRepository) =>
        _configElementRepository = configElementRepository;

    public async Task<HealthCheckResult> Check()
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

    protected override string Title => "FFmpeg Reports";
}