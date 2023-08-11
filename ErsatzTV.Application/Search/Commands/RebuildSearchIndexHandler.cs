using System.Diagnostics;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Repositories.Caching;
using ErsatzTV.Core.Interfaces.Search;
using Humanizer;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Search;

public class RebuildSearchIndexHandler : IRequestHandler<RebuildSearchIndex>
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IFallbackMetadataProvider _fallbackMetadataProvider;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger<RebuildSearchIndexHandler> _logger;
    private readonly ISearchIndex _searchIndex;
    private readonly ICachingSearchRepository _searchRepository;
    private readonly SystemStartup _systemStartup;

    public RebuildSearchIndexHandler(
        ISearchIndex searchIndex,
        ICachingSearchRepository searchRepository,
        IConfigElementRepository configElementRepository,
        ILocalFileSystem localFileSystem,
        IFallbackMetadataProvider fallbackMetadataProvider,
        SystemStartup systemStartup,
        ILogger<RebuildSearchIndexHandler> logger)
    {
        _searchIndex = searchIndex;
        _logger = logger;
        _searchRepository = searchRepository;
        _configElementRepository = configElementRepository;
        _localFileSystem = localFileSystem;
        _fallbackMetadataProvider = fallbackMetadataProvider;
        _systemStartup = systemStartup;
    }

    public async Task Handle(RebuildSearchIndex request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing search index");

        bool indexExists = await _searchIndex.IndexExists();

        if (!await _searchIndex.Initialize(_localFileSystem, _configElementRepository))
        {
            indexExists = false;
        }

        _logger.LogInformation("Done initializing search index");

        if (!indexExists ||
            await _configElementRepository.GetValue<int>(ConfigElementKey.SearchIndexVersion) <
            _searchIndex.Version)
        {
            _logger.LogInformation("Migrating search index to version {Version}", _searchIndex.Version);

            var sw = Stopwatch.StartNew();
            await _searchIndex.Rebuild(_searchRepository, _fallbackMetadataProvider);

            await _configElementRepository.Upsert(ConfigElementKey.SearchIndexVersion, _searchIndex.Version);
            sw.Stop();

            _logger.LogInformation("Done migrating search index in {Duration}", sw.Elapsed.Humanize());
        }
        else
        {
            _logger.LogInformation("Search index is already version {Version}", _searchIndex.Version);
        }

        _systemStartup.SearchIndexIsReady();
    }
}
