using MediatR;

namespace ErsatzTV.Application.Search.Queries
{
    public record QuerySearchIndexAllItems(string Query) : IRequest<SearchResultAllItemsViewModel>;
}
