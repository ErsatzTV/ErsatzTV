using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public class Playout
    {
        public int Id { get; set; }
        public int ChannelId { get; set; }
        public Channel Channel { get; set; }
        public int ProgramScheduleId { get; set; }
        public ProgramSchedule ProgramSchedule { get; set; }
        public ProgramSchedulePlayoutType ProgramSchedulePlayoutType { get; set; }
        public List<PlayoutItem> Items { get; set; }
        public PlayoutAnchor Anchor { get; set; }
        public List<PlayoutProgramScheduleAnchor> ProgramScheduleAnchors { get; set; }
    }
}
