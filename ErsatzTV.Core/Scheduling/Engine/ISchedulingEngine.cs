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
    Task<ISchedulingEngine> AddCollection(string key, string collectionName, PlaybackOrder playbackOrder);
    Task<ISchedulingEngine> AddMultiCollection(string key, string multiCollectionName, PlaybackOrder playbackOrder);
    Task<ISchedulingEngine> AddSmartCollection(string key, string smartCollectionName, PlaybackOrder playbackOrder);
    Task<ISchedulingEngine> AddSearch(string key, string query, PlaybackOrder playbackOrder);
    Task<ISchedulingEngine> AddShow(string key, Dictionary<string, string> guids, PlaybackOrder playbackOrder);

    // content instructions
    ISchedulingEngine AddCount(
        string content,
        int count,
        Option<FillerKind> fillerKind,
        string customTitle,
        bool disableWatermarks);

    // control instructions
    ISchedulingEngine WaitUntil(TimeOnly waitUntil, bool tomorrow, bool rewindOnReset);



    PlayoutAnchor GetAnchor();

    ISchedulingEngineState GetState();
}
