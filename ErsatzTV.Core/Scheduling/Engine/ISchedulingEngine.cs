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
    Task AddCollection(string key, string collectionName, PlaybackOrder playbackOrder);

    Task AddMarathon(
        string key,
        Dictionary<string, List<string>> guids,
        List<string> searches,
        string groupBy,
        bool shuffleGroups,
        PlaybackOrder itemPlaybackOrder,
        bool playAllItems);

    Task AddMultiCollection(string key, string multiCollectionName, PlaybackOrder playbackOrder);
    Task AddPlaylist(string key, string playlist, string playlistGroup);
    Task AddSmartCollection(string key, string smartCollectionName, PlaybackOrder playbackOrder);
    Task AddSearch(string key, string query, PlaybackOrder playbackOrder);
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

    // control instructions
    void LockGuideGroup(bool advance);
    void UnlockGuideGroup();
    Task GraphicsOn(List<string> graphicsElements, Dictionary<string, string> variables);
    Task GraphicsOff(List<string> graphicsElements);
    Task WatermarkOn(List<string> watermarks);
    Task WatermarkOff(List<string> watermarks);
    void SkipItems(string content, int count);
    void SkipToItem(string content, int season, int episode);
    ISchedulingEngine WaitUntil(TimeOnly waitUntil, bool tomorrow, bool rewindOnReset);



    PlayoutAnchor GetAnchor();

    ISchedulingEngineState GetState();
}
