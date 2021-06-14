namespace ErsatzTV.Application.ProgramSchedules.Commands
{
    public record CreateProgramScheduleResult(int ProgramScheduleId) : EntityIdResult(ProgramScheduleId);
}
