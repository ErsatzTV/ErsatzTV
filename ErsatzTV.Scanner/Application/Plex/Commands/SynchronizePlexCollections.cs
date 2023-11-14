using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.Plex;

public record SynchronizePlexCollections(int PlexMediaSourceId, bool ForceScan) : IRequest<Either<BaseError, Unit>>;
