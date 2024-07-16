using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IPlayoutTimeShifter
{
    public void TimeShift(Playout playout, DateTimeOffset now, bool force);
}
