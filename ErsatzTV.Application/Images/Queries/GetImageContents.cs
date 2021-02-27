using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Images.Queries
{
    public record GetImageContents(string FileName, ArtworkKind ArtworkKind, int? MaxHeight = null) : IRequest<Either<BaseError, ImageViewModel>>;
}
