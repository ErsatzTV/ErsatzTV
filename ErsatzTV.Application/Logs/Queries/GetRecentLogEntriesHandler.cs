using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Metadata;
using static ErsatzTV.Application.Logs.Mapper;

namespace ErsatzTV.Application.Logs;

public class GetRecentLogEntriesHandler : IRequestHandler<GetRecentLogEntries, PagedLogEntriesViewModel>
{
    private readonly ILocalFileSystem _localFileSystem;

    public GetRecentLogEntriesHandler(ILocalFileSystem localFileSystem) => _localFileSystem = localFileSystem;

    public Task<PagedLogEntriesViewModel> Handle(
        GetRecentLogEntries request,
        CancellationToken cancellationToken)
    {
        // get most recent file
        string logFileName = _localFileSystem.ListFiles(FileSystemLayout.LogsFolder)
            .OrderDescending()
            .FirstOrDefault();

        if (logFileName is not null)
        {
            IQueryable<LogEntryViewModel> entries = ReadFrom(logFileName)
                .Bind(line => ProjectToViewModel(line))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Filter))
            {
                entries = entries.Filter(
                    le => le.Level.ToString().Contains(request.Filter, StringComparison.OrdinalIgnoreCase) ||
                          le.Message.Contains(request.Filter, StringComparison.OrdinalIgnoreCase));
            }

            int count = entries.Count();

            IOrderedQueryable<LogEntryViewModel> ordered = request.SortDescending.Match(
                descending => descending
                    ? entries.OrderByDescending(request.SortExpression).ThenByDescending(le => le.Timestamp)
                    : entries.OrderBy(request.SortExpression).ThenByDescending(le => le.Timestamp),
                () => entries.OrderByDescending(le => le.Timestamp));

            var page = ordered
                .Skip(request.PageNum * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new PagedLogEntriesViewModel(count, page).AsTask();
        }

        return new PagedLogEntriesViewModel(0, new List<LogEntryViewModel>()).AsTask();
    }

    private static IEnumerable<string> ReadFrom(string file)
    {
        using FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fs);
        while (reader.ReadLine() is { } line)
        {
            yield return line;
        }
    }
}
