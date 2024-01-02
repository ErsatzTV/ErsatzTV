using Destructurama.Attributed;

namespace ErsatzTV.Core.Domain;

public class PlayoutScheduleItemFillGroupIndex
{
    public int Id { get; set; }

    public int PlayoutId { get; set; }

    [NotLogged]
    public Playout Playout { get; set; }
    
    public int ProgramScheduleItemId { get; set; }
    
    [NotLogged]
    public ProgramScheduleItem ProgramScheduleItem { get; set; }

    public CollectionEnumeratorState EnumeratorState { get; set; }
}
