using System.Collections.Generic;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Emby.Commands
{
    public record SynchronizeEmbyMediaSources : IRequest<Either<BaseError, List<EmbyMediaSource>>>,
        IEmbyBackgroundServiceRequest;
}
