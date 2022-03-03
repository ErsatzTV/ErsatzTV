using ErsatzTV.Core;

namespace ErsatzTV.Application.ProgramSchedules;

public record DeleteProgramSchedule(int ProgramScheduleId) : IRequest<Either<BaseError, LanguageExt.Unit>>;