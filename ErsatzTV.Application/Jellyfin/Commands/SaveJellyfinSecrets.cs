using ErsatzTV.Core;
using ErsatzTV.Core.Jellyfin;

namespace ErsatzTV.Application.Jellyfin;

public record SaveJellyfinSecrets(JellyfinSecrets Secrets) : MediatR.IRequest<Either<BaseError, Unit>>;