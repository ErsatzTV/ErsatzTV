using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Application.Search.Commands
{
    public record AddItemsToSearchIndex(List<MediaItem> MediaItems) : MediatR.IRequest<Unit>,
        ISearchBackgroundServiceRequest;
}
