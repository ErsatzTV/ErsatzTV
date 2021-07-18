using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record DeleteMultiCollection(int MultiCollectionId) : MediatR.IRequest<Either<BaseError, Unit>>;
}
