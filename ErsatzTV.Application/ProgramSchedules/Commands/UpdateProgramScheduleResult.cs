namespace ErsatzTV.Application.ProgramSchedules.Commands
{
    public record UpdateProgramScheduleResult(int ProgramScheduleId) : EntityIdResult(ProgramScheduleId);
}
