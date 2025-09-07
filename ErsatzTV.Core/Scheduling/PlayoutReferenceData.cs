using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Core.Scheduling;

public record PlayoutReferenceData(
    Channel Channel,
    Option<Deco> Deco,
    List<PlayoutItem> ExistingItems,
    List<PlayoutTemplate> PlayoutTemplates,
    ProgramSchedule ProgramSchedule,
    List<ProgramScheduleAlternate> ProgramScheduleAlternates,
    List<PlayoutHistory> PlayoutHistory,
    TimeSpan MinPlayoutOffset);
