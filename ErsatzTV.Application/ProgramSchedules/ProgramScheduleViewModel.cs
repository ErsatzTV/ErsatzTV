namespace ErsatzTV.Application.ProgramSchedules;

public record ProgramScheduleViewModel(
    int Id,
    string Name,
    bool KeepMultiPartEpisodesTogether,
    bool TreatCollectionsAsShows,
    bool ShuffleScheduleItems);
