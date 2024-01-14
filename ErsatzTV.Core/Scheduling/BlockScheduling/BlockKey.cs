using System.Diagnostics.CodeAnalysis;
using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Core.Scheduling.BlockScheduling;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public record BlockKey
{
    public BlockKey()
    {
    }

    public BlockKey(Block block, Template template, PlayoutTemplate playoutTemplate)
    {
        b = block.Id;
        bt = block.DateUpdated.Ticks;
        t = template.Id;
        tt = template.DateUpdated.Ticks;
        pt = playoutTemplate.Id;
        ptt = playoutTemplate.DateUpdated.Ticks;
    }

    /// <summary>
    /// Block Id
    /// </summary>
    public int b { get; set; }
    
    /// <summary>
    /// Template Id
    /// </summary>
    public int t { get; set; }
    
    /// <summary>
    /// Playout Template Id
    /// </summary>
    public int pt { get; set; }

    /// <summary>
    /// Block Date Updated Ticks
    /// </summary>
    public long bt { get; set; }
    
    /// <summary>
    /// Template Date Updated Ticks
    /// </summary>
    public long tt { get; set; }
    
    /// <summary>
    /// Playout Template Date Updated Ticks
    /// </summary>
    public long ptt { get; set; }
}
