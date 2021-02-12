using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class LogRepository : ILogRepository
    {
        private readonly LogContext _logContext;

        public LogRepository(LogContext logContext) => _logContext = logContext;

        public Task<List<LogEntry>> GetRecentLogEntries() =>
            _logContext.LogEntries.OrderByDescending(e => e.Id).Take(100).ToListAsync();
    }
}
