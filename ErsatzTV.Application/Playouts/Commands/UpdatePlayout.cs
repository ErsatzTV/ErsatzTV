using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Playouts.Commands
{
    public record UpdatePlayout(
        int PlayoutId,
        int ChannelId,
        int ProgramScheduleId,
        ProgramSchedulePlayoutType ProgramSchedulePlayoutType) : IRequest<Either<BaseError, PlayoutViewModel>>;
}
