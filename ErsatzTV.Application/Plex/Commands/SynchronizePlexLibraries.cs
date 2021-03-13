using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Plex.Commands
{
    public record SynchronizePlexLibraries(int PlexMediaSourceId) : MediatR.IRequest<Either<BaseError, Unit>>,
        IPlexBackgroundServiceRequest;
}
