using ErsatzTV.Application.ProgramSchedules;

namespace ErsatzTV.ViewModels;

public class ProgramScheduleEditViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool KeepMultiPartEpisodesTogether { get; set; }
    public bool TreatCollectionsAsShows { get; set; }
    public bool ShuffleScheduleItems { get; set; }
    public bool RandomStartPoint { get; set; }

    public UpdateProgramSchedule ToUpdate() =>
        new(Id, Name, KeepMultiPartEpisodesTogether, TreatCollectionsAsShows, ShuffleScheduleItems, RandomStartPoint);

    public CreateProgramSchedule ToCreate() =>
        new(Name, KeepMultiPartEpisodesTogether, TreatCollectionsAsShows, ShuffleScheduleItems, RandomStartPoint);
}
