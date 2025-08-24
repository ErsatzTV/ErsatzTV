using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Scheduling.Engine;

public interface ISchedulingEngine
{
    ISchedulingEngine WithMode(PlayoutBuildMode mode);

    ISchedulingEngine WithSeed(int seed);

    ISchedulingEngine WithReferenceData(PlayoutReferenceData referenceData);

    ISchedulingEngine BuildBetween(DateTimeOffset start, DateTimeOffset finish);

    ISchedulingEngine RemoveBefore(DateTimeOffset removeBefore);

    ISchedulingEngine RestoreOrReset(Option<PlayoutAnchor> maybeAnchor);

    Task<ISchedulingEngine> AddCollection(string key, string collectionName, PlaybackOrder playbackOrder);



    ISchedulingEngineState GetState();
}
