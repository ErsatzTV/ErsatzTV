namespace ErsatzTV.Application.ProgramSchedules;

public record PagedProgramSchedulesViewModel(int TotalCount, List<ProgramScheduleViewModel> Page);
