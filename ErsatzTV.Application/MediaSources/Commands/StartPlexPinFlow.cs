using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaSources.Commands
{
    public record StartPlexPinFlow : IRequest<Either<BaseError, string>>;
}
