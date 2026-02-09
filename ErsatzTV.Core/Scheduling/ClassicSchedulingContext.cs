namespace ErsatzTV.Core.Scheduling;

public record ClassicSchedulingContext(
    string Scheduler,
    int ScheduleId,
    int ItemId,
    string Enumerator,
    int Seed,
    int Index);
