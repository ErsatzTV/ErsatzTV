using System.Collections.Concurrent;
using ErsatzTV.Core.Interfaces.Metadata;
using MediatR;

namespace ErsatzTV.Core.Metadata;

public class ScannerProxyService(IMediator mediator) : IScannerProxyService
{
    private readonly ConcurrentDictionary<int, decimal> _activeLibraries = [];
    private readonly ConcurrentDictionary<Guid, int> _scans = new();

    public Option<Guid> StartScan(int libraryId)
    {
        if (!_activeLibraries.TryAdd(libraryId, 0))
        {
            return Option<Guid>.None;
        }

        var buildId = Guid.NewGuid();
        _scans[buildId] = libraryId;
        return buildId;
    }

    public void EndScan(Guid scanId)
    {
        if (_scans.TryRemove(scanId, out int libraryId))
        {
            _activeLibraries.TryRemove(libraryId, out _);
        }
    }

    public async Task Progress(Guid scanId, decimal percentComplete)
    {
        //logger.LogInformation("Scanning {ScanId}", scanId);

        if (_scans.TryGetValue(scanId, out int libraryId))
        {
            //logger.LogDebug("Scan progress {Progress} for library {LibraryId}", percentComplete, libraryId);

            _activeLibraries[libraryId] = percentComplete;

            var progress = new LibraryScanProgress(libraryId, percentComplete);
            await mediator.Publish(progress);
        }
    }

    public bool IsActive(Guid scanId) => _scans.ContainsKey(scanId);

    public Option<decimal> GetProgress(int libraryId) => _activeLibraries.TryGetValue(libraryId, out decimal progress)
        ? progress
        : Option<decimal>.None;
}
