using System.Collections.Generic;
using LanguageExt;

namespace ErsatzTV.Application.Search.Commands
{
    public record RemoveItemsFromSearchIndex(List<int> MediaItemIds) : MediatR.IRequest<Unit>,
        ISearchBackgroundServiceRequest;
}
