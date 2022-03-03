using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.ProgramSchedules;

public record CreateProgramSchedule(
    string Name,
    bool KeepMultiPartEpisodesTogether,
    bool TreatCollectionsAsShows,
    bool ShuffleScheduleItems) : IRequest<Either<BaseError, CreateProgramScheduleResult>>;