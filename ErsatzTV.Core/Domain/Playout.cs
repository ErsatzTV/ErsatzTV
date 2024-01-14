using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Core.Domain;

public class Playout
{
    public int Id { get; set; }
    public int ChannelId { get; set; }
    public Channel Channel { get; set; }
    public int? ProgramScheduleId { get; set; }
    public ProgramSchedule ProgramSchedule { get; set; }
    public string ExternalJsonFile { get; set; }
    public List<ProgramScheduleAlternate> ProgramScheduleAlternates { get; set; }
    public ProgramSchedulePlayoutType ProgramSchedulePlayoutType { get; set; }
    public List<PlayoutItem> Items { get; set; }
    public PlayoutAnchor Anchor { get; set; }
    public List<PlayoutProgramScheduleAnchor> ProgramScheduleAnchors { get; set; }
    public List<PlayoutScheduleItemFillGroupIndex> FillGroupIndices { get; set; }
    public ICollection<PlayoutTemplate> Templates { get; set; }
    public ICollection<PlayoutHistory> PlayoutHistory { get; set; }
    public TimeSpan? DailyRebuildTime { get; set; }
}
