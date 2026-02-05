namespace ErsatzTV.Core.Scheduling;

public record SchedulingContext(
    string Scheduler,
    int ScheduleId,
    int ItemId,
    string Enumerator,
    int Seed,
    int Index);
