using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Maintenance.Commands
{
    public record DeleteOrphanedArtwork : MediatR.IRequest<Either<BaseError, Unit>>, IBackgroundServiceRequest;
}
