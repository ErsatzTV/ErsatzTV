using ErsatzTV.Core;
using ErsatzTV.Core.Jellyfin;

namespace ErsatzTV.Application.Jellyfin;

public record SaveJellyfinSecrets(JellyfinSecrets Secrets) : IRequest<Either<BaseError, Unit>>;
