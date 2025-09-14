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
    private CollectionType _collectionType;
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

            if (_fillerKind is not FillerKind.MidRoll)
            {
                Expression = string.Empty;
            }

            if (_fillerKind is FillerKind.Fallback)
            {
                UseChaptersAsMediaItems = false;
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
        get => FillerMode is FillerMode.Count or FillerMode.RandomCount ? _count : null;
        set => _count = value;
    }

    public int? PadToNearestMinute
    {
        get => FillerMode == FillerMode.Pad ? _padToNearestMinute : null;
        set => _padToNearestMinute = value;
    }

    public bool AllowWatermarks { get; set; }

    public CollectionType CollectionType
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
            if (_collectionType is CollectionType.Playlist)
            {
                _fillerMode = FillerMode.Count;
            }
        }
    }

    public MediaCollectionViewModel Collection { get; set; }
    public NamedMediaItemViewModel MediaItem { get; set; }
    public MultiCollectionViewModel MultiCollection { get; set; }
    public SmartCollectionViewModel SmartCollection { get; set; }
    public PlaylistViewModel Playlist { get; set; }

    public string Expression { get; set; }

    public bool UseChaptersAsMediaItems { get; set; }

    public IRequest<Either<BaseError, Unit>> ToEdit() =>
        new UpdateFillerPreset(
            Id,
            Name,
            FillerKind,
            FillerMode,
            Duration.Map(FixDuration),
            Count,
            PadToNearestMinute,
            AllowWatermarks,
            CollectionType,
            Collection?.Id,
            MediaItem?.MediaItemId,
            MultiCollection?.Id,
            SmartCollection?.Id,
            Playlist?.Id,
            Expression,
            UseChaptersAsMediaItems);

    public IRequest<Either<BaseError, Unit>> ToUpdate() =>
        new CreateFillerPreset(
            Name,
            FillerKind,
            FillerMode,
            Duration.Map(FixDuration),
            Count,
            PadToNearestMinute,
            AllowWatermarks,
            CollectionType,
            Collection?.Id,
            MediaItem?.MediaItemId,
            MultiCollection?.Id,
            SmartCollection?.Id,
            Playlist?.Id,
            Expression,
            UseChaptersAsMediaItems);

    private static TimeSpan FixDuration(TimeSpan duration) =>
        duration > TimeSpan.FromDays(1) ? duration.Subtract(TimeSpan.FromDays(1)) : duration;
}
