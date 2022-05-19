using ErsatzTV.Core;
using ErsatzTV.Core.Emby;

namespace ErsatzTV.Application.Emby;

public record SaveEmbySecrets(EmbySecrets Secrets) : IRequest<Either<BaseError, Unit>>;
