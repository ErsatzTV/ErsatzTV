using ErsatzTV.Core;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Application.ProgramSchedules;

public record CreateProgramSchedule(
    string Name,
    bool KeepMultiPartEpisodesTogether,
    bool TreatCollectionsAsShows,
    bool ShuffleScheduleItems,
    bool RandomStartPoint,
    FixedStartTimeBehavior FixedStartTimeBehavior) : IRequest<Either<BaseError, CreateProgramScheduleResult>>;
