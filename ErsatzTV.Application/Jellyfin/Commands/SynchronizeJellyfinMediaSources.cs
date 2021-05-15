using System.Collections.Generic;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Jellyfin.Commands
{
    public record SynchronizeJellyfinMediaSources : IRequest<Either<BaseError, List<JellyfinMediaSource>>>,
        IJellyfinBackgroundServiceRequest;
}
