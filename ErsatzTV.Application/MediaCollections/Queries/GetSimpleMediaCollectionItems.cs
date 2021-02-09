using System.Collections.Generic;
using ErsatzTV.Application.MediaItems;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Queries
{
    public record GetSimpleMediaCollectionItems(int Id) : IRequest<Option<IEnumerable<MediaItemViewModel>>>;
}
