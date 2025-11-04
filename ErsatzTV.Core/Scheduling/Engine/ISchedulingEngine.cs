using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;

namespace ErsatzTV.Core.Scheduling.Engine;

public interface ISchedulingEngine
{
    ISchedulingEngine WithPlayoutId(int playoutId);

    ISchedulingEngine WithMode(PlayoutBuildMode mode);

    ISchedulingEngine WithSeed(int seed);

    ISchedulingEngine WithReferenceData(PlayoutReferenceData referenceData);

    ISchedulingEngine BuildBetween(DateTimeOffset start, DateTimeOffset finish);

    ISchedulingEngine RemoveBefore(DateTimeOffset removeBefore);

    ISchedulingEngine RestoreOrReset(Option<PlayoutAnchor> maybeAnchor);

    // content definitions
    Task AddCollection(
        string key,
        string collectionName,
        PlaybackOrder playbackOrder,
        CancellationToken cancellationToken);

    Task AddMarathon(
        string key,
        Dictionary<string, List<string>> guids,
        List<string> searches,
        string groupBy,
        bool shuffleGroups,
        PlaybackOrder itemPlaybackOrder,
        bool playAllItems);

    Task AddMultiCollection(
        string key,
        string multiCollectionName,
        PlaybackOrder playbackOrder,
        CancellationToken cancellationToken);

    Task AddPlaylist(string key, string playlist, string playlistGroup, CancellationToken cancellationToken);
    Task CreatePlaylist(string key, Dictionary<string, int> playlistItems, CancellationToken cancellationToken);

    Task AddSmartCollection(
        string key,
        string smartCollectionName,
        PlaybackOrder playbackOrder,
        CancellationToken cancellationToken);

    Task AddSearch(string key, string query, PlaybackOrder playbackOrder, CancellationToken cancellationToken);
    Task AddShow(string key, Dictionary<string, string> guids, PlaybackOrder playbackOrder);

    // content instructions
    bool AddAll(
        string content,
        Option<FillerKind> fillerKind,
        string customTitle,
        bool disableWatermarks);

    bool AddCount(
        string content,
        int count,
        Option<FillerKind> fillerKind,
        string customTitle,
        bool disableWatermarks);

    bool AddDuration(
        string content,
        string duration,
        string fallback,
        bool trim,
        int discardAttempts,
        bool stopBeforeEnd,
        bool offlineTail,
        Option<FillerKind> maybeFillerKind,
        string customTitle,
        bool disableWatermarks);

    bool PadToNext(
        string content,
        int minutes,
        string fallback,
        bool trim,
        int discardAttempts,
        bool stopBeforeEnd,
        bool offlineTail,
        Option<FillerKind> maybeFillerKind,
        string customTitle,
        bool disableWatermarks);

    bool PadUntil(
        string content,
        string padUntil,
        bool tomorrow,
        string fallback,
        bool trim,
        int discardAttempts,
        bool stopBeforeEnd,
        bool offlineTail,
        Option<FillerKind> maybeFillerKind,
        string customTitle,
        bool disableWatermarks);

    bool PadUntilExact(
        string content,
        DateTimeOffset padUntil,
        string fallback,
        bool trim,
        int discardAttempts,
        bool stopBeforeEnd,
        bool offlineTail,
        Option<FillerKind> maybeFillerKind,
        string customTitle,
        bool disableWatermarks);

    Option<MediaItem> PeekNext(string content);

    // control instructions
    void LockGuideGroup(bool advance, string customTitle);
    void UnlockGuideGroup();

    Task GraphicsOn(
        List<string> graphicsElements,
        Dictionary<string, string> variables,
        CancellationToken cancellationToken);

    Task GraphicsOff(List<string> graphicsElements, CancellationToken cancellationToken);
    Task WatermarkOn(List<string> watermarks);
    Task WatermarkOff(List<string> watermarks);
    void PreRollOn(string content);
    void PreRollOff();

    void SkipItems(string content, int count);
    void SkipToItem(string content, int season, int episode);
    ISchedulingEngine WaitUntil(TimeOnly waitUntil, bool tomorrow, bool rewindOnReset);
    ISchedulingEngine WaitUntilExact(DateTimeOffset waitUntil, bool rewindOnReset);



    PlayoutAnchor GetAnchor();

    ISchedulingEngineState GetState();
}
