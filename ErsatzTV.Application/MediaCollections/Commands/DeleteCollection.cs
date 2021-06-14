using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record DeleteCollection(int CollectionId) : IRequest<Either<BaseError, LanguageExt.Unit>>;
}
