using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Logs.Queries
{
    public record GetRecentLogEntries : IRequest<List<LogEntryViewModel>>;
}
