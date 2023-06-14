using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Interfaces.Scripting;
using ErsatzTV.Core.Scheduling;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Infrastructure.Scheduling;

public class MultiEpisodeShuffleCollectionEnumerator : IMediaCollectionEnumerator
{
    private readonly CancellationToken _cancellationToken;
    private readonly IList<MediaItem> _mediaItems;
    private readonly IScriptEngine _scriptEngine;
    private readonly string _scriptFile;
    private readonly ILogger _logger;
    private readonly int _mediaItemCount;
    private readonly Dictionary<int, List<MediaItem>> _mediaItemGroups;
    private readonly List<MediaItem> _ungrouped;
    private CloneableRandom _random;
    private IList<MediaItem> _shuffled;
    private readonly Lazy<Option<TimeSpan>> _lazyMinimumDuration;

    public MultiEpisodeShuffleCollectionEnumerator(
        IList<MediaItem> mediaItems,
        CollectionEnumeratorState state,
        IScriptEngine scriptEngine,
        string scriptFile,
        ILogger logger,
        CancellationToken cancellationToken,
        bool skipLoad = false)
    {
        _mediaItems = mediaItems;
        _scriptEngine = scriptEngine;
        _scriptFile = scriptFile;
        _logger = logger;
        _cancellationToken = cancellationToken;

        if (!skipLoad)
        {
            scriptEngine.Load(scriptFile);
        }

        var numParts = (int)(double)scriptEngine.GetValue("numParts");

        _mediaItemGroups = new Dictionary<int, List<MediaItem>>();
        for (var i = 1; i <= numParts; i++)
        {
            _mediaItemGroups.Add(i, new List<MediaItem>());
        }

        _ungrouped = new List<MediaItem>();
        _mediaItemCount = mediaItems.Count;

        IList<Episode> validEpisodes = mediaItems
            .OfType<Episode>()
            .Filter(e => e.Season is not null && e.EpisodeMetadata is not null && e.EpisodeMetadata.Count == 1)
            .ToList();
        foreach (Episode episode in validEpisodes)
        {
            // prep script params
            int seasonNumber = episode.Season.SeasonNumber;
            int episodeNumber = episode.EpisodeMetadata[0].EpisodeNumber;

            // call the script function, and if we get a part (group) number back, use it
            if (scriptEngine.Invoke("partNumberForEpisode", seasonNumber, episodeNumber) is double result)
            {
                _mediaItemGroups[(int)result].Add(episode);
            }
            else
            {
                _ungrouped.Add(episode);
            }
        }

        // add everything else
        _ungrouped.AddRange(mediaItems.Except(validEpisodes));

        if (state.Index >= _mediaItemCount)
        {
            state.Index = 0;
            state.Seed = new Random(state.Seed).Next();
        }

        _random = new CloneableRandom(state.Seed);
        _shuffled = Shuffle(_random);
        _lazyMinimumDuration = new Lazy<Option<TimeSpan>>(() => _shuffled.Bind(i => i.GetDuration()).HeadOrNone());

        State = new CollectionEnumeratorState { Seed = state.Seed };
        while (State.Index < state.Index)
        {
            MoveNext();
        }
    }

    public IMediaCollectionEnumerator Clone(CollectionEnumeratorState state, CancellationToken cancellationToken)
    {
        return new MultiEpisodeShuffleCollectionEnumerator(
            _mediaItems,
            state,
            _scriptEngine,
            _scriptFile,
            _logger,
            cancellationToken,
            skipLoad: true);
    }

    public CollectionEnumeratorState State { get; }

    public Option<MediaItem> Current => _shuffled.Any() ? _shuffled[State.Index % _mediaItemCount] : None;

    public void MoveNext()
    {
        if ((State.Index + 1) % _mediaItemCount == 0)
        {
            Option<MediaItem> tail = Current;

            State.Index = 0;
            do
            {
                State.Seed = _random.Next();
                _random = new CloneableRandom(State.Seed);
                _shuffled = Shuffle(_random);
            } while (!_cancellationToken.IsCancellationRequested && _mediaItemCount > 1 &&
                     Current.Map(x => x.Id) == tail.Map(x => x.Id));
        }
        else
        {
            State.Index++;
        }

        State.Index %= _mediaItemCount;
    }

    private IList<MediaItem> Shuffle(CloneableRandom random)
    {
        int maxGroupNumber = _mediaItemGroups.Max(a => a.Key);
        var shuffledGroups = new List<IList<MediaItem>>();
        for (var i = 1; i <= maxGroupNumber; i++)
        {
            shuffledGroups.Add(Shuffle(_mediaItemGroups[i], random));
        }

        int minItems = shuffledGroups.Min(g => g.Count);
        if (shuffledGroups.Any(g => g.Count != minItems))
        {
            _logger.LogError("Multi Episode Groups are different sizes; shuffle will not perform correctly!");
        }

        // convert shuffled "groups" into groups that can be used for scheduling
        var copy = new GroupedMediaItem[minItems + _ungrouped.Count];
        for (var i = 0; i < minItems; i++)
        {
            var group = new GroupedMediaItem(shuffledGroups[0][i], null);
            for (var j = 1; j < shuffledGroups.Count; j++)
            {
                group.Additional.Add(shuffledGroups[j][i]);
            }

            copy[i] = group;
        }

        // convert all ungrouped into groups that can be used for scheduling
        for (var i = 0; i < _ungrouped.Count; i++)
        {
            MediaItem ungrouped = _ungrouped[i];
            copy[minItems + i] = new GroupedMediaItem(ungrouped, null);
        }

        // perform shuffle
        int n = copy.Length;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            (copy[k], copy[n]) = (copy[n], copy[k]);
        }

        // flatten
        return GroupedMediaItem.FlattenGroups(copy, _mediaItemCount);
    }

    private static IList<MediaItem> Shuffle(IEnumerable<MediaItem> mediaItems, CloneableRandom random)
    {
        MediaItem[] copy = mediaItems.ToArray();

        int n = copy.Length;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            (copy[k], copy[n]) = (copy[n], copy[k]);
        }

        return copy;
    }

    public Option<TimeSpan> MinimumDuration => _lazyMinimumDuration.Value;
    
    public int Count => _shuffled.Count;
}
