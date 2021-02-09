using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaSources.Commands
{
    public record CreateLocalMediaSource
        (string Name, MediaType MediaType, string Folder) : IRequest<Either<BaseError, MediaSourceViewModel>>;
}
