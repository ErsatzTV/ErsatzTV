using ErsatzTV.Application.Images;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Plex.Queries
{
    public record GetPlexImage(int PlexMediaSourceId, string Path) : IRequest<Either<BaseError, ImageViewModel>>;
}
