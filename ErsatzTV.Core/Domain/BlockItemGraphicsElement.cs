using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Core.Domain;

public class BlockItemGraphicsElement
{
    public int BlockItemId { get; set; }
    public BlockItem BlockItem { get; set; }
    public int GraphicsElementId { get; set; }
    public GraphicsElement GraphicsElement { get; set; }
}
