using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record CreateCollection(string Name) : IRequest<Either<BaseError, MediaCollectionViewModel>>;
}
