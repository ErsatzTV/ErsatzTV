using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Emby.Commands
{
    public record DisconnectEmby : MediatR.IRequest<Either<BaseError, Unit>>;
}
