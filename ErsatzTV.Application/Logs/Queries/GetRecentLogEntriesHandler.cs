using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Logs.Mapper;

namespace ErsatzTV.Application.Logs.Queries
{
    public class GetRecentLogEntriesHandler : IRequestHandler<GetRecentLogEntries, List<LogEntryViewModel>>
    {
        private readonly ILogRepository _logRepository;

        public GetRecentLogEntriesHandler(ILogRepository logRepository) => _logRepository = logRepository;

        public Task<List<LogEntryViewModel>> Handle(GetRecentLogEntries request, CancellationToken cancellationToken) =>
            _logRepository.GetRecentLogEntries().Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
