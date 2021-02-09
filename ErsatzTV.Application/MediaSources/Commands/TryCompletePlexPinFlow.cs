using ErsatzTV.Core;
using ErsatzTV.Core.Plex;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaSources.Commands
{
    public record TryCompletePlexPinFlow(PlexAuthPin AuthPin) : IRequest<Either<BaseError, bool>>,
        IPlexBackgroundServiceRequest;
}
