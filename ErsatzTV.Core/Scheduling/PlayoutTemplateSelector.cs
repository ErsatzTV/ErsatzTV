using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Core.Scheduling;

public static class PlayoutTemplateSelector
{
    public static Option<PlayoutTemplate> GetPlayoutTemplateFor(
        IEnumerable<PlayoutTemplate> templates,
        DateTimeOffset date)
    {
        foreach (PlayoutTemplate template in templates.OrderBy(x => x.Index))
        {
            if (template.StartDate.HasValue && template.EndDate.HasValue)
            {
                if (date.Date < template.StartDate.Value.Date || date.Date > template.EndDate.Value.Date)
                {
                    continue;
                }
            }
            
            bool daysOfWeek = template.DaysOfWeek.Contains(date.DayOfWeek);
            if (!daysOfWeek)
            {
                continue;
            }

            bool daysOfMonth = template.DaysOfMonth.Contains(date.Day);
            if (!daysOfMonth)
            {
                continue;
            }

            bool monthOfYear = template.MonthsOfYear.Contains(date.Month);
            if (monthOfYear)
            {
                return template;
            }
        }

        return Option<PlayoutTemplate>.None;
    }
}
