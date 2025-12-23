#nullable enable
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.Controllers.Api;

// Flat collection response (no inheritance from MediaCardViewModel)
public record CollectionApiResponse(
    int Id,
    string Name,
    CollectionType CollectionType,
    bool UseCustomPlaybackOrder,
    MediaItemState State);

// Flat smart collection response
public record SmartCollectionApiResponse(
    int Id,
    string Name,
    string Query);

// Flat multi-collection response with inline items
public record MultiCollectionApiResponse(
    int Id,
    string Name,
    List<MultiCollectionItemApiResponse> Items,
    List<MultiCollectionSmartItemApiResponse> SmartItems);

public record MultiCollectionItemApiResponse(
    int MultiCollectionId,
    int CollectionId,
    string CollectionName,
    CollectionType CollectionType,
    bool UseCustomPlaybackOrder,
    MediaItemState State,
    bool ScheduleAsGroup,
    PlaybackOrder PlaybackOrder);

public record MultiCollectionSmartItemApiResponse(
    int MultiCollectionId,
    int SmartCollectionId,
    string SmartCollectionName,
    string Query,
    bool ScheduleAsGroup,
    PlaybackOrder PlaybackOrder);

// Flat media item response
public record MediaItemApiResponse(
    int MediaItemId,
    string Name);

// Block Item API Response - all data, flat structure
public record BlockItemApiResponse(
    int Id,
    int Index,
    CollectionType CollectionType,
    CollectionApiResponse? Collection,
    MultiCollectionApiResponse? MultiCollection,
    SmartCollectionApiResponse? SmartCollection,
    MediaItemApiResponse? MediaItem,
    string? SearchTitle,
    string? SearchQuery,
    PlaybackOrder PlaybackOrder,
    bool IncludeInProgramGuide,
    bool DisableWatermarks,
    List<WatermarkApiResponse> Watermarks,
    List<GraphicsElementApiResponse> GraphicsElements);

public record WatermarkApiResponse(
    int Id,
    string Name,
    string? ImagePath,
    ChannelWatermarkMode Mode,
    ChannelWatermarkImageSource ImageSource,
    WatermarkLocation Location,
    WatermarkSize Size,
    double Width,
    double HorizontalMargin,
    double VerticalMargin,
    int FrequencyMinutes,
    int DurationSeconds,
    int Opacity,
    bool PlaceWithinSourceContent,
    string? OpacityExpression,
    int ZIndex);

public record GraphicsElementApiResponse(
    int Id,
    string Name,
    string FileName);

// Playlist Item API Response - all data, flat structure
public record PlaylistItemApiResponse(
    int Id,
    int Index,
    CollectionType CollectionType,
    CollectionApiResponse? Collection,
    MultiCollectionApiResponse? MultiCollection,
    SmartCollectionApiResponse? SmartCollection,
    MediaItemApiResponse? MediaItem,
    PlaybackOrder PlaybackOrder,
    int? Count,
    bool PlayAll,
    bool IncludeInProgramGuide);

// Playlist response
public record PlaylistApiResponse(
    int Id,
    int PlaylistGroupId,
    string Name,
    bool IsSystem);

// Rerun collection response
public record RerunCollectionApiResponse(
    int Id,
    string Name);

// Filler preset response - flat
public record FillerPresetApiResponse(
    int Id,
    string Name);

// Schedule Item API Response - all data, flat structure
public record ScheduleItemApiResponse(
    int Id,
    int Index,
    StartType StartType,
    TimeSpan? StartTime,
    FixedStartTimeBehavior? FixedStartTimeBehavior,
    PlayoutMode PlayoutMode,
    CollectionType CollectionType,
    CollectionApiResponse? Collection,
    MultiCollectionApiResponse? MultiCollection,
    SmartCollectionApiResponse? SmartCollection,
    RerunCollectionApiResponse? RerunCollection,
    PlaylistApiResponse? Playlist,
    MediaItemApiResponse? MediaItem,
    string? SearchTitle,
    string? SearchQuery,
    PlaybackOrder PlaybackOrder,
    MarathonGroupBy MarathonGroupBy,
    bool MarathonShuffleGroups,
    bool MarathonShuffleItems,
    int? MarathonBatchSize,
    FillWithGroupMode FillWithGroupMode,
    string? CustomTitle,
    GuideMode GuideMode,
    FillerPresetApiResponse? PreRollFiller,
    FillerPresetApiResponse? MidRollFiller,
    FillerPresetApiResponse? PostRollFiller,
    FillerPresetApiResponse? TailFiller,
    FillerPresetApiResponse? FallbackFiller,
    List<WatermarkApiResponse> Watermarks,
    List<GraphicsElementApiResponse> GraphicsElements,
    string? PreferredAudioLanguageCode,
    string? PreferredAudioTitle,
    string? PreferredSubtitleLanguageCode,
    ChannelSubtitleMode? SubtitleMode,
    int? Count,
    TimeSpan? PlayoutDuration);

// Paged response types
public record PagedCollectionsApiResponse(int TotalCount, List<CollectionApiResponse> Page);

// Deco API Response - all data, flat structure
public record DecoApiResponse(
    int Id,
    int DecoGroupId,
    string DecoGroupName,
    string Name,
    DecoMode WatermarkMode,
    List<WatermarkApiResponse> Watermarks,
    bool UseWatermarkDuringFiller,
    DecoMode GraphicsElementsMode,
    List<GraphicsElementApiResponse> GraphicsElements,
    bool UseGraphicsElementsDuringFiller,
    DecoMode BreakContentMode,
    List<DecoBreakContentApiResponse> BreakContent,
    DecoMode DefaultFillerMode,
    CollectionType DefaultFillerCollectionType,
    CollectionApiResponse? DefaultFillerCollection,
    MediaItemApiResponse? DefaultFillerMediaItem,
    MultiCollectionApiResponse? DefaultFillerMultiCollection,
    SmartCollectionApiResponse? DefaultFillerSmartCollection,
    bool DefaultFillerTrimToFit,
    DecoMode DeadAirFallbackMode,
    CollectionType DeadAirFallbackCollectionType,
    CollectionApiResponse? DeadAirFallbackCollection,
    MediaItemApiResponse? DeadAirFallbackMediaItem,
    MultiCollectionApiResponse? DeadAirFallbackMultiCollection,
    SmartCollectionApiResponse? DeadAirFallbackSmartCollection);

public record DecoBreakContentApiResponse(
    int Id,
    CollectionType CollectionType,
    CollectionApiResponse? Collection,
    MediaItemApiResponse? MediaItem,
    MultiCollectionApiResponse? MultiCollection,
    SmartCollectionApiResponse? SmartCollection,
    PlaylistApiResponse? Playlist,
    DecoBreakPlacement Placement);
