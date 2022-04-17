using System.Text;
using CliWrap;
using CliWrap.Buffered;
using ErsatzTV.Core.Health;

namespace ErsatzTV.Infrastructure.Health.Checks;

public abstract class BaseHealthCheck
{
    protected abstract string Title { get; }

    protected HealthCheckResult Result(HealthCheckStatus status, string message) =>
        new(Title, status, message, None);

    protected HealthCheckResult NotApplicableResult() =>
        new(Title, HealthCheckStatus.NotApplicable, string.Empty, None);
        
    protected HealthCheckResult OkResult() =>
        new(Title, HealthCheckStatus.Pass, string.Empty, None);

    protected HealthCheckResult FailResult(string message) =>
        new(Title, HealthCheckStatus.Fail, message, None);

    protected HealthCheckResult WarningResult(string message) =>
        new(Title, HealthCheckStatus.Warning, message, None);

    protected HealthCheckResult WarningResult(string message, string link) =>
        new(Title, HealthCheckStatus.Warning, message, link);

    protected HealthCheckResult InfoResult(string message) =>
        new(Title, HealthCheckStatus.Info, message, None);

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