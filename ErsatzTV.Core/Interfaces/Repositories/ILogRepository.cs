using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface ILogRepository
    {
        public Task<List<LogEntry>> GetRecentLogEntries();
    }
}
