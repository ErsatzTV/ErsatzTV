using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record CreateSmartCollection
        (string Query, string Name) : IRequest<Either<BaseError, SmartCollectionViewModel>>;
}
