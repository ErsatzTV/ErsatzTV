namespace ErsatzTV.Application.ProgramSchedules;

public record GetProgramScheduleById(int Id) : IRequest<Option<ProgramScheduleViewModel>>;
