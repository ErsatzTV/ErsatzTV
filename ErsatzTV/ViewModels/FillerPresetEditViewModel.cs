using ErsatzTV.Application.Filler;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using MediatR;

namespace ErsatzTV.ViewModels;

public class FillerPresetEditViewModel
{
    private ProgramScheduleItemCollectionType _collectionType;
    private int? _count;
    private TimeSpan? _duration;
    private FillerKind _fillerKind;
    private FillerMode _fillerMode;
    private int? _padToNearestMinute;

    public int Id { get; set; }
    public string Name { get; set; }

    public FillerKind FillerKind
    {
        get => _fillerKind;
        set
        {
            _fillerKind = value;
            if (_fillerKind is FillerKind.Fallback or FillerKind.Tail)
            {
                FillerMode = FillerMode.None;
            }
        }
    }

    public FillerMode FillerMode
    {
        get => FillerKind is FillerKind.Fallback or FillerKind.Tail ? FillerMode.None : _fillerMode;
        set => _fillerMode = value;
    }

    public TimeSpan? Duration
    {
        get => FillerMode == FillerMode.Duration ? _duration : null;
        set => _duration = value;
    }

    public int? Count
    {
        get => FillerMode == FillerMode.Count ? _count : null;
        set => _count = value;
    }

    public int? PadToNearestMinute
    {
        get => FillerMode == FillerMode.Pad ? _padToNearestMinute : null;
        set => _padToNearestMinute = value;
    }

    public ProgramScheduleItemCollectionType CollectionType
    {
        get => _collectionType;
        set
        {
            if (_collectionType != value)
            {
                Collection = null;
                MediaItem = null;
                MultiCollection = null;
                SmartCollection = null;
            }

            _collectionType = value;
        }
    }

    public MediaCollectionViewModel Collection { get; set; }
    public NamedMediaItemViewModel MediaItem { get; set; }
    public MultiCollectionViewModel MultiCollection { get; set; }
    public SmartCollectionViewModel SmartCollection { get; set; }

    public IRequest<Either<BaseError, Unit>> ToEdit() =>
        new UpdateFillerPreset(
            Id,
            Name,
            FillerKind,
            FillerMode,
            Duration.Map(FixDuration),
            Count,
            PadToNearestMinute,
            CollectionType,
            Collection?.Id,
            MediaItem?.MediaItemId,
            MultiCollection?.Id,
            SmartCollection?.Id);

    public IRequest<Either<BaseError, Unit>> ToUpdate() =>
        new CreateFillerPreset(
            Name,
            FillerKind,
            FillerMode,
            Duration.Map(FixDuration),
            Count,
            PadToNearestMinute,
            CollectionType,
            Collection?.Id,
            MediaItem?.MediaItemId,
            MultiCollection?.Id,
            SmartCollection?.Id);

    private static TimeSpan FixDuration(TimeSpan duration) =>
        duration > TimeSpan.FromDays(1) ? duration.Subtract(TimeSpan.FromDays(1)) : duration;
}
