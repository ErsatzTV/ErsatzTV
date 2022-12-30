using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.Jellyfin;

public record SynchronizeJellyfinCollections(int JellyfinMediaSourceId) : IRequest<Either<BaseError, Unit>>;
