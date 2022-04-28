using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record UpdateCollection
    (int CollectionId, string Name) : IRequest<Either<BaseError, Unit>>
{
    public Option<bool> UseCustomPlaybackOrder { get; set; } = None;
}
