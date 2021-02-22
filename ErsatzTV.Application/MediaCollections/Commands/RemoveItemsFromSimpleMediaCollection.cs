using System.Collections.Generic;
using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record RemoveItemsFromSimpleMediaCollection
        (int MediaCollectionId) : MediatR.IRequest<Either<BaseError, Unit>>
    {
        public List<int> MovieIds { get; set; } = new();
        public List<int> TelevisionShowIds { get; set; } = new();
        public List<int> TelevisionSeasonIds { get; set; } = new();
        public List<int> TelevisionEpisodeIds { get; set; } = new();
    }
}
