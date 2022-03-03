using ErsatzTV.Application.MediaCards;

namespace ErsatzTV.Application.Search;

public record QuerySearchIndexArtists
    (string Query, int PageNumber, int PageSize) : IRequest<ArtistCardResultsViewModel>;