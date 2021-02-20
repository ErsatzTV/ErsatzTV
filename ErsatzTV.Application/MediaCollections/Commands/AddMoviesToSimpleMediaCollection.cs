using System.Collections.Generic;
using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record AddMoviesToSimpleMediaCollection
        (int MediaCollectionId, List<int> MovieIds) : MediatR.IRequest<Either<BaseError, Unit>>;
}
