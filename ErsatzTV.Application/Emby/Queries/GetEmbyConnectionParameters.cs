using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Emby.Queries
{
    public record GetEmbyConnectionParameters : IRequest<Either<BaseError, EmbyConnectionParametersViewModel>>;
}
