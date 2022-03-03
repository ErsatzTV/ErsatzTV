using ErsatzTV.Core;

namespace ErsatzTV.Application.Maintenance;

public record DeleteOrphanedArtwork : MediatR.IRequest<Either<BaseError, Unit>>, IBackgroundServiceRequest;