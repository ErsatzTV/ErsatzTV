using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public class ProgramSchedule
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public PlaybackOrder MediaCollectionPlaybackOrder { get; set; }
        public bool KeepMultiPartEpisodesTogether { get; set; }
        public List<ProgramScheduleItem> Items { get; set; }
        public List<Playout> Playouts { get; set; }
    }
}
