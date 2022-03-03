using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Playouts;

public record PlayoutViewModel(
    int Id,
    PlayoutChannelViewModel Channel,
    PlayoutProgramScheduleViewModel ProgramSchedule,
    ProgramSchedulePlayoutType ProgramSchedulePlayoutType);