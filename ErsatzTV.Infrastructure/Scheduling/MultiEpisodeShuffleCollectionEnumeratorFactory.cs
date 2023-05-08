using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Interfaces.Scripting;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Infrastructure.Scheduling;

public class MultiEpisodeShuffleCollectionEnumeratorFactory
    : IMultiEpisodeShuffleCollectionEnumeratorFactory
{
    private readonly IScriptEngine _scriptEngine;
    private readonly ILogger<MultiEpisodeShuffleCollectionEnumeratorFactory> _logger;

    public MultiEpisodeShuffleCollectionEnumeratorFactory(
        IScriptEngine scriptEngine,
        ILogger<MultiEpisodeShuffleCollectionEnumeratorFactory> logger)
    {
        _scriptEngine = scriptEngine;
        _logger = logger;
    }

    public IMediaCollectionEnumerator Create(
        string jsScriptPath,
        IList<MediaItem> mediaItems,
        CollectionEnumeratorState state,
        CancellationToken cancellationToken) =>
        new MultiEpisodeShuffleCollectionEnumerator(
            mediaItems,
            state,
            _scriptEngine,
            jsScriptPath,
            _logger,
            cancellationToken);
}
