using ErsatzTV.Core;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Images;

public record GetCachedImagePath(string FileName, ArtworkKind ArtworkKind, string ContentType, int? MaxHeight = null)
    : IRequest<
        Either<BaseError, CachedImagePathViewModel>>;
