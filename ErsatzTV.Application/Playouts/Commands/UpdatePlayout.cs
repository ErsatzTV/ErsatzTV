using System;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Playouts.Commands
{
    public record UpdatePlayout
        (int PlayoutId, Option<TimeSpan> DailyRebuildTime) : IRequest<Either<BaseError, PlayoutNameViewModel>>;
}
