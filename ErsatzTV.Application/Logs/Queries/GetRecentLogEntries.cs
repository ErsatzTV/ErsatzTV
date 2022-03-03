using System;
using System.Linq.Expressions;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Logs;

public record GetRecentLogEntries(int PageNum, int PageSize) : IRequest<PagedLogEntriesViewModel>
{
    public Expression<Func<LogEntry, object>> SortExpression { get; set; }
    public Option<bool> SortDescending { get; set; }
}