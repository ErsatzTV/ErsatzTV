namespace ErsatzTV.Application.Maintenance;

public record ReleaseMemory(bool ForceAggressive) : IRequest, IBackgroundServiceRequest
{
    public DateTimeOffset RequestTime = DateTimeOffset.Now;
}
