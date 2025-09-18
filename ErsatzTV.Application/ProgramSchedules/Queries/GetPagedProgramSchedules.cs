namespace ErsatzTV.Application.ProgramSchedules;

public record GetPagedProgramSchedules(string Query, int PageNum, int PageSize)
    : IRequest<PagedProgramSchedulesViewModel>;
