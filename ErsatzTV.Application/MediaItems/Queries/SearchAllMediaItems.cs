using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.MediaItems.Queries
{
    public record SearchAllMediaItems(string SearchString) : IRequest<List<MediaItemSearchResultViewModel>>;
}
