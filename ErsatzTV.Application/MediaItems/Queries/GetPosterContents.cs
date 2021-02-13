using ErsatzTV.Application.Images;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaItems.Queries
{
    public record GetPosterContents(int MediaItemId) : IRequest<Either<BaseError, ImageViewModel>>;
}
