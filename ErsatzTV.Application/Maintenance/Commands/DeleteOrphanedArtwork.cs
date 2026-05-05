using ErsatzTV.Core;

namespace ErsatzTV.Application.Maintenance;

public record DeleteOrphanedArtwork(int? MaxToDelete) : IRequest<Either<BaseError, Unit>>, IBackgroundServiceRequest;
