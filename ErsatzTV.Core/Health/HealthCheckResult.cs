namespace ErsatzTV.Core.Health;

public record HealthCheckResult(string Title, HealthCheckStatus Status, string Message, Option<string> Link);
