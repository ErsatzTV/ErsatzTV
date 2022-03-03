using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Playouts;

public record CreatePlayout(
    int ChannelId,
    int ProgramScheduleId,
    ProgramSchedulePlayoutType ProgramSchedulePlayoutType) : IRequest<Either<BaseError, CreatePlayoutResponse>>;