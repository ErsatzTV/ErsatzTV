using ErsatzTV.Application.MediaCards;
using MediatR;

namespace ErsatzTV.Application.Search;

public record QuerySearchIndexSongs
    (string Query, int PageNumber, int PageSize) : IRequest<SongCardResultsViewModel>;