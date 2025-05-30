using ErsatzTV.Application.ProgramSchedules;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.ViewModels;

public class ProgramScheduleEditViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool KeepMultiPartEpisodesTogether { get; set; }
    public bool TreatCollectionsAsShows { get; set; }
    public bool ShuffleScheduleItems { get; set; }
    public bool RandomStartPoint { get; set; }
    public FixedStartTimeBehavior FixedStartTimeBehavior { get; set; }

    public UpdateProgramSchedule ToUpdate() =>
        new(
            Id,
            Name,
            KeepMultiPartEpisodesTogether,
            TreatCollectionsAsShows,
            ShuffleScheduleItems,
            RandomStartPoint,
            FixedStartTimeBehavior);

    public CreateProgramSchedule ToCreate() =>
        new(
            Name,
            KeepMultiPartEpisodesTogether,
            TreatCollectionsAsShows,
            ShuffleScheduleItems,
            RandomStartPoint,
            FixedStartTimeBehavior);
}
