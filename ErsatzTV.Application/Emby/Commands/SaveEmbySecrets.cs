using ErsatzTV.Core;
using ErsatzTV.Core.Emby;

namespace ErsatzTV.Application.Emby;

public record SaveEmbySecrets(EmbySecrets Secrets) : MediatR.IRequest<Either<BaseError, Unit>>;