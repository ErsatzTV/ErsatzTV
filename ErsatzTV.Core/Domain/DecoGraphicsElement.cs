using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Core.Domain;

public class DecoGraphicsElement
{
    public int DecoId { get; set; }
    public Deco Deco { get; set; }
    public int GraphicsElementId { get; set; }
    public GraphicsElement GraphicsElement { get; set; }
}
