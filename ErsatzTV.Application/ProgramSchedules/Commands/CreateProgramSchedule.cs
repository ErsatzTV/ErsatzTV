using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.ProgramSchedules.Commands
{
    public record CreateProgramSchedule(string Name, PlaybackOrder MediaCollectionPlaybackOrder) :
        IRequest<Either<BaseError, ProgramScheduleViewModel>>;
}
