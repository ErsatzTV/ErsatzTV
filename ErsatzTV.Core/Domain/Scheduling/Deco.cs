namespace ErsatzTV.Core.Domain.Scheduling;

public class Deco
{
    public int Id { get; set; }
    public int DecoGroupId { get; set; }
    public DecoGroup DecoGroup { get; set; }

    public string Name { get; set; }

    // watermark
    public DecoMode WatermarkMode { get; set; }
    public List<ChannelWatermark> Watermarks { get; set; }
    public List<DecoWatermark> DecoWatermarks { get; set; }
    public bool UseWatermarkDuringFiller { get; set; }

    // graphics elements
    public DecoMode GraphicsElementsMode { get; set; }
    public List<GraphicsElement> GraphicsElements { get; set; }
    public List<DecoGraphicsElement> DecoGraphicsElements { get; set; }
    public bool UseGraphicsElementsDuringFiller { get; set; }

    // default filler
    public DecoMode DefaultFillerMode { get; set; }
    public ProgramScheduleItemCollectionType DefaultFillerCollectionType { get; set; }
    public int? DefaultFillerCollectionId { get; set; }
    public Collection DefaultFillerCollection { get; set; }
    public int? DefaultFillerMediaItemId { get; set; }
    public MediaItem DefaultFillerMediaItem { get; set; }
    public int? DefaultFillerMultiCollectionId { get; set; }
    public MultiCollection DefaultFillerMultiCollection { get; set; }
    public int? DefaultFillerSmartCollectionId { get; set; }
    public SmartCollection DefaultFillerSmartCollection { get; set; }
    public bool DefaultFillerTrimToFit { get; set; }

    // dead air fallback
    public DecoMode DeadAirFallbackMode { get; set; }
    public ProgramScheduleItemCollectionType DeadAirFallbackCollectionType { get; set; }
    public int? DeadAirFallbackCollectionId { get; set; }
    public Collection DeadAirFallbackCollection { get; set; }
    public int? DeadAirFallbackMediaItemId { get; set; }
    public MediaItem DeadAirFallbackMediaItem { get; set; }
    public int? DeadAirFallbackMultiCollectionId { get; set; }
    public MultiCollection DeadAirFallbackMultiCollection { get; set; }
    public int? DeadAirFallbackSmartCollectionId { get; set; }
    public SmartCollection DeadAirFallbackSmartCollection { get; set; }

    // can be added directly to (block) playouts
    public ICollection<Playout> Playouts { get; set; }
}
