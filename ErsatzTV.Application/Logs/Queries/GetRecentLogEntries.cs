using System.Linq.Expressions;

namespace ErsatzTV.Application.Logs;

public record GetRecentLogEntries(int PageNum, int PageSize, string Filter) : IRequest<PagedLogEntriesViewModel>
{
    public Expression<Func<LogEntryViewModel, object>> SortExpression { get; init; }
    public Option<bool> SortDescending { get; init; }
}
