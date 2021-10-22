using MediatR;

namespace ErsatzTV.Application.Filler.Queries
{
    public record GetPagedFillerPresets(int PageNum, int PageSize) : IRequest<PagedFillerPresetsViewModel>;
}
