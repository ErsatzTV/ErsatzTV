using ErsatzTV.Core;

namespace ErsatzTV.Application.ProgramSchedules;

public record CreateProgramSchedule(
    string Name,
    bool KeepMultiPartEpisodesTogether,
    bool TreatCollectionsAsShows,
    bool ShuffleScheduleItems,
    bool RandomStartPoint) : IRequest<Either<BaseError, CreateProgramScheduleResult>>;
