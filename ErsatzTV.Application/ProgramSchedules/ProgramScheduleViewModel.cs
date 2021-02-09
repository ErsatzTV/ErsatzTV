using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.ProgramSchedules
{
    public record ProgramScheduleViewModel(int Id, string Name, PlaybackOrder MediaCollectionPlaybackOrder);
}
