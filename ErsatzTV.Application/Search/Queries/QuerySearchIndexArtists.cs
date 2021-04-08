using ErsatzTV.Application.MediaCards;
using MediatR;

namespace ErsatzTV.Application.Search.Queries
{
    public record QuerySearchIndexArtists
        (string Query, int PageNumber, int PageSize) : IRequest<ArtistCardResultsViewModel>;
}
