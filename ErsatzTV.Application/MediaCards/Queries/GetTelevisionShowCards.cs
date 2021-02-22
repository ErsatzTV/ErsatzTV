using MediatR;

namespace ErsatzTV.Application.MediaCards.Queries
{
    public record GetTelevisionShowCards(int PageNumber, int PageSize) : IRequest<TelevisionShowCardResultsViewModel>;
}
