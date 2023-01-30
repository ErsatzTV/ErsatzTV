using ErsatzTV.Core;

namespace ErsatzTV.Application.ProgramSchedules;

public record CopyProgramSchedule
    (int ProgramScheduleId, string Name) : IRequest<Either<BaseError, ProgramScheduleViewModel>>;
