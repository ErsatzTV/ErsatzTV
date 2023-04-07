using ErsatzTV.Core;

namespace ErsatzTV.Application.Maintenance;

public record DeleteOrphanedSubtitles : IRequest<Either<BaseError, Unit>>, IBackgroundServiceRequest;
