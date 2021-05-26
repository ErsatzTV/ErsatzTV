using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Images.Queries
{
    public record GetCachedImagePath
        (string FileName, ArtworkKind ArtworkKind, int? MaxHeight = null) : IRequest<
            Either<BaseError, CachedImagePathViewModel>>;
}
