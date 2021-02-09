using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record CreateSimpleMediaCollection
        (string Name) : IRequest<Either<BaseError, MediaCollectionViewModel>>;
}
