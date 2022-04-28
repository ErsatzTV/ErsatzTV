using System.Diagnostics;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using Humanizer;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Search;

public class RebuildSearchIndexHandler : IRequestHandler<RebuildSearchIndex, Unit>
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger<RebuildSearchIndexHandler> _logger;
    private readonly ISearchIndex _searchIndex;
    private readonly ISearchRepository _searchRepository;

    public RebuildSearchIndexHandler(
        ISearchIndex searchIndex,
        ISearchRepository searchRepository,
        IConfigElementRepository configElementRepository,
        ILocalFileSystem localFileSystem,
        ILogger<RebuildSearchIndexHandler> logger)
    {
        _searchIndex = searchIndex;
        _logger = logger;
        _searchRepository = searchRepository;
        _configElementRepository = configElementRepository;
        _localFileSystem = localFileSystem;
    }

    public async Task<Unit> Handle(RebuildSearchIndex request, CancellationToken cancellationToken)
    {
        bool indexFolderExists = Directory.Exists(FileSystemLayout.SearchIndexFolder);

        await _searchIndex.Initialize(_localFileSystem, _configElementRepository);

        if (!indexFolderExists ||
            await _configElementRepository.GetValue<int>(ConfigElementKey.SearchIndexVersion) <
            _searchIndex.Version)
        {
            _logger.LogInformation("Migrating search index to version {Version}", _searchIndex.Version);

            var sw = Stopwatch.StartNew();
            await _searchIndex.Rebuild(_searchRepository);

            await _configElementRepository.Upsert(ConfigElementKey.SearchIndexVersion, _searchIndex.Version);
            sw.Stop();

            _logger.LogInformation("Done migrating search index in {Duration}", sw.Elapsed.Humanize());
        }
        else
        {
            _logger.LogInformation("Search index is already version {Version}", _searchIndex.Version);
        }

        return Unit.Default;
    }
}
