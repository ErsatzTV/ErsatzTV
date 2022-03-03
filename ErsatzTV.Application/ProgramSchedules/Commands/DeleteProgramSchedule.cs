using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.ProgramSchedules;

public record DeleteProgramSchedule(int ProgramScheduleId) : IRequest<Either<BaseError, LanguageExt.Unit>>;