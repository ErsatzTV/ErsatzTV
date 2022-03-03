using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections;

public record AddArtistToCollection
    (int CollectionId, int ArtistId) : MediatR.IRequest<Either<BaseError, Unit>>;