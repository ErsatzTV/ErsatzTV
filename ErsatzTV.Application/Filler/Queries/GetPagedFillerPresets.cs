namespace ErsatzTV.Application.Filler;

public record GetPagedFillerPresets(int PageNum, int PageSize) : IRequest<PagedFillerPresetsViewModel>;