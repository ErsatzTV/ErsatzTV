using ErsatzTV.Core;

namespace ErsatzTV.Application.ProgramSchedules;

public record CreateProgramSchedule(
    string Name,
    bool KeepMultiPartEpisodesTogether,
    bool TreatCollectionsAsShows,
    bool ShuffleScheduleItems) : IRequest<Either<BaseError, CreateProgramScheduleResult>>;