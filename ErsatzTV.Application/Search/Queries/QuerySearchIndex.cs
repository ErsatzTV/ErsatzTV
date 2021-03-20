using ErsatzTV.Core.Search;
using MediatR;

namespace ErsatzTV.Application.Search.Queries
{
    public record QuerySearchIndex(string Query) : IRequest<SearchResult>;
}
