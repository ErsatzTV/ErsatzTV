using ErsatzTV.Core;

namespace ErsatzTV.Application.ProgramSchedules;

public record UpdateProgramSchedule
(
    int ProgramScheduleId,
    string Name,
    bool KeepMultiPartEpisodesTogether,
    bool TreatCollectionsAsShows,
    bool ShuffleScheduleItems) : IRequest<Either<BaseError, UpdateProgramScheduleResult>>;