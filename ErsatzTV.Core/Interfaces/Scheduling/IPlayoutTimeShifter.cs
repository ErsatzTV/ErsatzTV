namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IPlayoutTimeShifter
{
    public Task TimeShift(int playoutId, DateTimeOffset now, bool force);
}
