using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Plex.Commands
{
    public record StartPlexPinFlow : IRequest<Either<BaseError, string>>;
}
