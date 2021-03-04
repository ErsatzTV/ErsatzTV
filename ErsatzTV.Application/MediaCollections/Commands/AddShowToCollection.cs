using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record AddShowToCollection
        (int CollectionId, int ShowId) : IRequest<Either<BaseError, CollectionUpdateResult>>;
}
