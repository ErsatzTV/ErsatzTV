using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;

namespace ErsatzTV.Core.Scheduling.TemplateScheduling;

public class PlayoutTemplateSchedulerPadToNext : PlayoutTemplateScheduler
{
    public static DateTimeOffset Schedule(
        Playout playout,
        DateTimeOffset currentTime,
        PlayoutTemplateCountItem count,
        IMediaCollectionEnumerator enumerator)
    {
        return currentTime;
    }
}
