using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.ViewModels;

public class BlockItemsEditViewModel
{
    public string Name { get; set; }
    public int Minutes { get; set; }
    public BlockStopScheduling StopScheduling { get; set; }
    public List<BlockItemEditViewModel> Items { get; set; }
}
