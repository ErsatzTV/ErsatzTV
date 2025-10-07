using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using ErsatzTV.Core.Interfaces.Database;

namespace ErsatzTV.Infrastructure.Health.Checks;

public class DowngradeHealthCheck(IDatabaseMigrations databaseMigrations) : BaseHealthCheck, IDowngradeHealthCheck
{
    public override string Title => "ErsatzTV Downgrade";

    public async Task<HealthCheckResult> Check(CancellationToken cancellationToken)
    {
        IReadOnlyList<string> unknownMigrations = await databaseMigrations.GetUnknownMigrations();
        if (unknownMigrations.Any())
        {
            return FailResult(
                "Downgrade detected; THIS IS NOT SUPPORTED AND WILL IMPACT STABILITY",
                "Downgrade detected",
                new HealthCheckLink("https://ersatztv.org/docs/installation/#downgrading"));
        }

        return NotApplicableResult();
    }
}
