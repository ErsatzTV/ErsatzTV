using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Infrastructure.Scheduling;

public class MultiEpisodeShuffleCollectionEnumeratorFactory
    : IMultiEpisodeShuffleCollectionEnumeratorFactory
{
    private readonly ILogger<MultiEpisodeShuffleCollectionEnumeratorFactory> _logger;

    public MultiEpisodeShuffleCollectionEnumeratorFactory(
        ILogger<MultiEpisodeShuffleCollectionEnumeratorFactory> logger)
    {
        _logger = logger;
    }

    public IMediaCollectionEnumerator Create(
        string luaScriptPath,
        IList<MediaItem> mediaItems,
        CollectionEnumeratorState state) =>
        new MultiEpisodeShuffleCollectionEnumerator(mediaItems, state, luaScriptPath, _logger);
}
