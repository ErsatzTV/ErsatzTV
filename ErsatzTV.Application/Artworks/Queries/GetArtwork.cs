using ErsatzTV.Core;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Artworks;

public record GetArtwork(int Id) : IRequest<Either<BaseError, Artwork>>;
