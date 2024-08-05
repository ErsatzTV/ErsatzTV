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
    public bool UseWatermarkDuringFiller { get; set; }

    public DecoMode DefaultFillerMode { get; set; }
    public ProgramScheduleItemCollectionType DefaultFillerCollectionType { get; set; }
    public MediaCollectionViewModel DefaultFillerCollection { get; set; }
    public MultiCollectionViewModel DefaultFillerMultiCollection { get; set; }
    public SmartCollectionViewModel DefaultFillerSmartCollection { get; set; }
    public NamedMediaItemViewModel DefaultFillerMediaItem { get; set; }
    public bool DefaultFillerTrimToFit { get; set; }

    public DecoMode DeadAirFallbackMode { get; set; }
    public ProgramScheduleItemCollectionType DeadAirFallbackCollectionType { get; set; }
    public MediaCollectionViewModel DeadAirFallbackCollection { get; set; }
    public MultiCollectionViewModel DeadAirFallbackMultiCollection { get; set; }
    public SmartCollectionViewModel DeadAirFallbackSmartCollection { get; set; }
    public NamedMediaItemViewModel DeadAirFallbackMediaItem { get; set; }
}
