using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Emby.Commands
{
    public record SynchronizeEmbyLibraries(int EmbyMediaSourceId) : MediatR.IRequest<Either<BaseError, Unit>>,
        IEmbyBackgroundServiceRequest;
}
