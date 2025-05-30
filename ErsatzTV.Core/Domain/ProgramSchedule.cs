﻿using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Core.Domain;

public class ProgramSchedule
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool KeepMultiPartEpisodesTogether { get; set; }
    public bool TreatCollectionsAsShows { get; set; }
    public bool ShuffleScheduleItems { get; set; }
    public bool RandomStartPoint { get; set; }
    public FixedStartTimeBehavior FixedStartTimeBehavior { get; set; }
    public List<ProgramScheduleItem> Items { get; set; }
    public List<Playout> Playouts { get; set; }
    public List<ProgramScheduleAlternate> ProgramScheduleAlternates { get; set; }
}
