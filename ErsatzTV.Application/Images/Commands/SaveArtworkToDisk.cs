using ErsatzTV.Core;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Images;

// ReSharper disable once SuggestBaseTypeForParameter
public record SaveArtworkToDisk(Stream Stream, ArtworkKind ArtworkKind) : IRequest<Either<BaseError, string>>;