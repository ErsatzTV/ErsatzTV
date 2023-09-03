using System.ComponentModel;
using System.Runtime.CompilerServices;
using ErsatzTV.Annotations;
using ErsatzTV.Application.Filler;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Application.Watermarks;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.ViewModels;

public class ProgramScheduleItemEditViewModel : INotifyPropertyChanged
{
    private ProgramScheduleItemCollectionType _collectionType;
    private int? _discardToFillAttempts;
    private int? _multipleCount;
    private TimeSpan? _playoutDuration;
    private int _playoutDurationHours;
    private int _playoutDurationMinutes;
    private TimeSpan? _startTime;

    public int Id { get; set; }
    public int Index { get; set; }
    public StartType StartType { get; set; }

    public TimeSpan? StartTime
    {
        get => StartType == StartType.Fixed ? _startTime : null;
        set => _startTime = value;
    }

    public PlayoutMode PlayoutMode { get; set; }

    public ProgramScheduleItemCollectionType CollectionType
    {
        get => _collectionType;
        set
        {
            if (_collectionType != value)
            {
                _collectionType = value;

                Collection = null;
                MultiCollection = null;
                MediaItem = null;
                SmartCollection = null;

                OnPropertyChanged(nameof(Collection));
                OnPropertyChanged(nameof(MultiCollection));
                OnPropertyChanged(nameof(MediaItem));
                OnPropertyChanged(nameof(SmartCollection));
            }

            if (_collectionType == ProgramScheduleItemCollectionType.MultiCollection)
            {
                PlaybackOrder = PlaybackOrder.Shuffle;
            }
        }
    }

    public MediaCollectionViewModel Collection { get; set; }
    public MultiCollectionViewModel MultiCollection { get; set; }
    public SmartCollectionViewModel SmartCollection { get; set; }
    public NamedMediaItemViewModel MediaItem { get; set; }
    public FillerPresetViewModel PreRollFiller { get; set; }
    public FillerPresetViewModel MidRollFiller { get; set; }
    public FillerPresetViewModel PostRollFiller { get; set; }
    public FillerPresetViewModel TailFiller { get; set; }
    public FillerPresetViewModel FallbackFiller { get; set; }
    public WatermarkViewModel Watermark { get; set; }
    public string PreferredAudioLanguageCode { get; set; }
    public string PreferredAudioTitle { get; set; }
    public string PreferredSubtitleLanguageCode { get; set; }
    public ChannelSubtitleMode? SubtitleMode { get; set; }

    public string CollectionName => CollectionType switch
    {
        ProgramScheduleItemCollectionType.Collection => Collection?.Name,
        ProgramScheduleItemCollectionType.TelevisionShow => MediaItem?.Name,
        ProgramScheduleItemCollectionType.TelevisionSeason => MediaItem?.Name,
        ProgramScheduleItemCollectionType.Artist => MediaItem?.Name,
        ProgramScheduleItemCollectionType.MultiCollection => MultiCollection?.Name,
        ProgramScheduleItemCollectionType.SmartCollection => SmartCollection?.Name,
        _ => string.Empty
    };

    public PlaybackOrder PlaybackOrder { get; set; }

    public int? MultipleCount
    {
        get => PlayoutMode == PlayoutMode.Multiple ? _multipleCount : null;
        set => _multipleCount = value;
    }

    public TimeSpan? PlayoutDuration
    {
        get => PlayoutMode == PlayoutMode.Duration ? _playoutDuration : null;
        set
        {
            _playoutDuration = value;
            CheckPlayoutDuration();
        }
    }

    public int PlayoutDurationHours
    {
        get => _playoutDurationHours;
        set
        {
            _playoutDuration = TimeSpan.FromHours(value) + TimeSpan.FromMinutes(_playoutDuration?.Minutes ?? 0);
            CheckPlayoutDuration();
        }
    }

    public int PlayoutDurationMinutes
    {
        get => _playoutDurationMinutes;
        set
        {
            _playoutDuration = TimeSpan.FromHours(_playoutDuration?.Hours ?? 0) + TimeSpan.FromMinutes(value);
            CheckPlayoutDuration();
        }
    }

    public TailMode TailMode { get; set; }

    public int? DiscardToFillAttempts
    {
        get => PlayoutMode == PlayoutMode.Duration ? _discardToFillAttempts ?? 0 : null;
        set => _discardToFillAttempts = value;
    }

    public string CustomTitle { get; set; }

    public GuideMode GuideMode { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged(
        [CallerMemberName]
        string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void CheckPlayoutDuration()
    {
        _playoutDuration ??= TimeSpan.Zero;

        if (_playoutDuration > TimeSpan.FromHours(24))
        {
            _playoutDuration = TimeSpan.FromHours(24);
        }

        if (_playoutDuration < TimeSpan.FromMinutes(1))
        {
            _playoutDuration = TimeSpan.FromMinutes(1);
        }

        _playoutDurationHours = (int)_playoutDuration.Value.TotalHours;
        _playoutDurationMinutes = _playoutDuration.Value.Minutes;

        OnPropertyChanged(nameof(PlayoutDuration));
        OnPropertyChanged(nameof(PlayoutDurationHours));
        OnPropertyChanged(nameof(PlayoutDurationMinutes));
    }
}
