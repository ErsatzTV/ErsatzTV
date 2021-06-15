using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Logs.Mapper;

namespace ErsatzTV.Application.Logs.Queries
{
    public class GetRecentLogEntriesHandler : IRequestHandler<GetRecentLogEntries, PagedLogEntriesViewModel>
    {
        private readonly IDbContextFactory<LogContext> _dbContextFactory;

        public GetRecentLogEntriesHandler(IDbContextFactory<LogContext> dbContextFactory) =>
            _dbContextFactory = dbContextFactory;

        public async Task<PagedLogEntriesViewModel> Handle(
            GetRecentLogEntries request,
            CancellationToken cancellationToken)
        {
            await using LogContext logContext = _dbContextFactory.CreateDbContext();
            int count = await logContext.LogEntries.CountAsync(cancellationToken);

            IOrderedQueryable<LogEntry> ordered = logContext.LogEntries
                .OrderByDescending(le => le.Id);

            foreach (bool descending in request.SortDescending)
            {
                ordered = descending
                    ? logContext.LogEntries.OrderByDescending(request.SortExpression).ThenByDescending(le => le.Id)
                    : logContext.LogEntries.OrderBy(request.SortExpression).ThenByDescending(le => le.Id);
            }

            List<LogEntryViewModel> page = await ordered
                .Skip(request.PageNum * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken)
                .Map(list => list.Map(ProjectToViewModel).ToList());

            return new PagedLogEntriesViewModel(count, page);
        }
    }
}
