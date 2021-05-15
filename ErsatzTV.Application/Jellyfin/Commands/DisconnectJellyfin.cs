using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Jellyfin.Commands
{
    public record DisconnectJellyfin : MediatR.IRequest<Either<BaseError, Unit>>;
}
