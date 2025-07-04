using MediatR;

namespace ErsatzTV.Core.Health;

public record HealthCheckSummary(int Warnings, int Errors) : INotification;
