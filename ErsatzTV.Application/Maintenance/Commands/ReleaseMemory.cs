namespace ErsatzTV.Application.Maintenance;

public record ReleaseMemory(bool ForceAggressive) : IRequest<Unit>, IBackgroundServiceRequest
{
    public DateTimeOffset RequestTime = DateTimeOffset.Now;
}