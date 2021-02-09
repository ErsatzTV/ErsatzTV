using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaSources.Commands
{
    public record SynchronizePlexLibraries(int PlexMediaSourceId) : MediatR.IRequest<Either<BaseError, Unit>>;
}
