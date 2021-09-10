using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record DeleteSmartCollection(int SmartCollectionId) : MediatR.IRequest<Either<BaseError, Unit>>;
}
