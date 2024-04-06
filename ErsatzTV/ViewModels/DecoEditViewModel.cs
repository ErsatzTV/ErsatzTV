using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.ViewModels;

public class DecoEditViewModel
{
    public int DecoGroupId { get; set; }
    public string Name { get; set; }
    public DecoMode WatermarkMode { get; set; }
    public int? WatermarkId { get; set; }
    public DecoMode DeadAirFallbackMode { get; set; }
    public ProgramScheduleItemCollectionType DeadAirFallbackCollectionType { get; set; }
    public MediaCollectionViewModel DeadAirFallbackCollection { get; set; }
    public MultiCollectionViewModel DeadAirFallbackMultiCollection { get; set; }
    public SmartCollectionViewModel DeadAirFallbackSmartCollection { get; set; }
    public NamedMediaItemViewModel DeadAirFallbackMediaItem { get; set; }
}
