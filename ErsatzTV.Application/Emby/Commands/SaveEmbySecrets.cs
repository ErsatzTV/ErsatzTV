using ErsatzTV.Core;
using ErsatzTV.Core.Emby;
using LanguageExt;

namespace ErsatzTV.Application.Emby.Commands
{
    public record SaveEmbySecrets(EmbySecrets Secrets) : MediatR.IRequest<Either<BaseError, Unit>>;
}
