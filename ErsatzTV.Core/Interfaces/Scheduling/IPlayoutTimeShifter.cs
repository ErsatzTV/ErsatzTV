namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IPlayoutTimeShifter
{
    Task TimeShift(int playoutId, DateTimeOffset now, bool force, CancellationToken cancellationToken);
}
