using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Search.Commands
{
    public class RebuildSearchIndexHandler : MediatR.IRequestHandler<RebuildSearchIndex, Unit>
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

            await _searchIndex.Initialize(_localFileSystem);

            if (!indexFolderExists ||
                await _configElementRepository.GetValue<int>(ConfigElementKey.SearchIndexVersion) <
                _searchIndex.Version)
            {
                _logger.LogDebug("Migrating search index to version {Version}", _searchIndex.Version);

                List<int> itemIds = await _searchRepository.GetItemIdsToIndex();
                await _searchIndex.Rebuild(_searchRepository, itemIds);

                Option<ConfigElement> maybeVersion =
                    await _configElementRepository.Get(ConfigElementKey.SearchIndexVersion);
                await maybeVersion.Match(
                    version =>
                    {
                        version.Value = _searchIndex.Version.ToString();
                        return _configElementRepository.Update(version);
                    },
                    () =>
                    {
                        var configElement = new ConfigElement
                        {
                            Key = ConfigElementKey.SearchIndexVersion.Key,
                            Value = _searchIndex.Version.ToString()
                        };
                        return _configElementRepository.Add(configElement);
                    });

                _logger.LogDebug("Done migrating search index");
            }
            else
            {
                _logger.LogDebug("Search index is already version {Version}", _searchIndex.Version);
            }

            return Unit.Default;
        }
    }
}
