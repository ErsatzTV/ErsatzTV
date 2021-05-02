using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Jellyfin.Commands
{
    public record SynchronizeJellyfinLibraries(int JellyfinMediaSourceId) : MediatR.IRequest<Either<BaseError, Unit>>,
        IJellyfinBackgroundServiceRequest;
}
