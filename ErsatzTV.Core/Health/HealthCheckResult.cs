namespace ErsatzTV.Core.Health;

public record HealthCheckResult(string Title, HealthCheckStatus Status, string Message, string BriefMessage, Option<HealthCheckLink> Link);
