using ErsatzTV.Core;

namespace ErsatzTV.Application.Maintenance;

public record DeleteOrphanedArtwork : IRequest<Either<BaseError, Unit>>, IBackgroundServiceRequest;
