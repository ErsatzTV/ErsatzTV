using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Jellyfin.Queries
{
    public record GetJellyfinConnectionParameters : IRequest<Either<BaseError, JellyfinConnectionParametersViewModel>>;
}
