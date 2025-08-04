namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IYamlScheduleValidator
{
    Task<bool> ValidateSchedule(string yaml, bool isImport);
}
