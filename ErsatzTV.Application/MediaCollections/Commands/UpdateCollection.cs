using ErsatzTV.Core;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.MediaCollections;

public record UpdateCollection
    (int CollectionId, string Name) : MediatR.IRequest<Either<BaseError, Unit>>
{
    public Option<bool> UseCustomPlaybackOrder { get; set; } = None;
}