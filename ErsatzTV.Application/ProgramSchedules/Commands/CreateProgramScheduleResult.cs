namespace ErsatzTV.Application.ProgramSchedules;

public record CreateProgramScheduleResult(int ProgramScheduleId) : EntityIdResult(ProgramScheduleId);