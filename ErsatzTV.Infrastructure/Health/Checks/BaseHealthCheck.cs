using System.Text;
using CliWrap;
using CliWrap.Buffered;
using ErsatzTV.Core.Health;

namespace ErsatzTV.Infrastructure.Health.Checks;

public abstract class BaseHealthCheck
{
    public abstract string Title { get; }

    protected HealthCheckResult Result(HealthCheckStatus status, string message, string briefMessage) =>
        new(Title, status, message, briefMessage, None);

    protected HealthCheckResult NotApplicableResult() =>
        new(Title, HealthCheckStatus.NotApplicable, string.Empty, string.Empty, None);

    protected HealthCheckResult OkResult() =>
        new(Title, HealthCheckStatus.Pass, string.Empty, string.Empty,  None);

    protected HealthCheckResult FailResult(string message, string briefMessage) =>
        new(Title, HealthCheckStatus.Fail, message, briefMessage, None);

    protected HealthCheckResult WarningResult(string message, string briefMessage) =>
        new(Title, HealthCheckStatus.Warning, message, briefMessage, None);

    protected HealthCheckResult WarningResult(string message, string briefMessage, HealthCheckLink link) =>
        new(Title, HealthCheckStatus.Warning, message, briefMessage, link);

    protected HealthCheckResult InfoResult(string message, string briefMessage) =>
        new(Title, HealthCheckStatus.Info, message, briefMessage, None);

    protected static async Task<string> GetProcessOutput(
        string path,
        IEnumerable<string> arguments,
        CancellationToken cancellationToken)
    {
        BufferedCommandResult result = await Cli.Wrap(path)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(Encoding.UTF8, cancellationToken);

        return result.StandardOutput;
    }
}
