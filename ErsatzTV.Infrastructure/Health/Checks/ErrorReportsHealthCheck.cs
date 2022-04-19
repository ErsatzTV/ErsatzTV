using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using Microsoft.Extensions.Options;

namespace ErsatzTV.Infrastructure.Health.Checks;

public class ErrorReportsHealthCheck : BaseHealthCheck, IErrorReportsHealthCheck
{
    private readonly IOptions<BugsnagConfiguration> _bugsnagConfiguration;

    public ErrorReportsHealthCheck(IOptions<BugsnagConfiguration> bugsnagConfiguration) =>
        _bugsnagConfiguration = bugsnagConfiguration;

    protected override string Title => "Error Reports";

    public Task<HealthCheckResult> Check(CancellationToken cancellationToken)
    {
        if (_bugsnagConfiguration.Value.Enable)
        {
            return Result(
                    HealthCheckStatus.Pass,
                    "Automated error reporting is enabled, thank you! To disable, edit the file appsettings.json or set the Bugsnag:Enable environment variable to false")
                .AsTask();
        }

        return InfoResult("Automated error reporting is disabled. Please enable to support bug fixing efforts!")
            .AsTask();
    }
}
