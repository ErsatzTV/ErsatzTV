using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.ProgramSchedules.Commands
{
    public record UpdateProgramSchedule
    (
        int ProgramScheduleId,
        string Name,
        PlaybackOrder MediaCollectionPlaybackOrder,
        bool KeepMultiPartEpisodesTogether) : IRequest<Either<BaseError, ProgramScheduleViewModel>>;
}
