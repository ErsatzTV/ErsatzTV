namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IYamlScheduleValidator
{
    Task<bool> ValidateSchedule(string yaml, bool isImport);
    string ToJson(string yaml);
    Task<IList<string>> GetValidationMessages(string yaml, bool isImport);
}
