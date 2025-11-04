using System.ComponentModel;
using System.Runtime.CompilerServices;
using ErsatzTV.Annotations;
using ErsatzTV.Application.Filler;
using ErsatzTV.Application.Graphics;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Application.Watermarks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.ViewModels;

public class ProgramScheduleItemEditViewModel : INotifyPropertyChanged
{
    private CollectionType _collectionType;
    private int? _discardToFillAttempts;
    private FixedStartTimeBehavior? _fixedStartTimeBehavior;
    private int? _multipleCount;
    private PlaybackOrder _playbackOrder;
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

    public FixedStartTimeBehavior? FixedStartTimeBehavior
    {
        get => StartType == StartType.Fixed ? _fixedStartTimeBehavior : null;
        set => _fixedStartTimeBehavior = value;
    }

    public FillWithGroupMode FillWithGroupMode { get; set; }

    public bool CanFillWithGroups =>
        PlayoutMode is PlayoutMode.Multiple or PlayoutMode.Duration
        && PlaybackOrder is not PlaybackOrder.ShuffleInOrder
        && CollectionType is CollectionType.Collection or CollectionType.MultiCollection
            or CollectionType.SmartCollection;

    public PlayoutMode PlayoutMode { get; set; }

    public CollectionType CollectionType
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
                SearchTitle = null;
                SearchQuery = null;
                RerunCollection = null;

                if (_collectionType != CollectionType.Playlist &&
                    MultipleMode is MultipleMode.PlaylistItemSize)
                {
                    MultipleMode = MultipleMode.Count;
                }

                if (_collectionType is CollectionType.Playlist
                    or CollectionType.RerunFirstRun
                    or CollectionType.RerunRerun)
                {
                    PlaybackOrder = PlaybackOrder.None;
                }

                OnPropertyChanged(nameof(Collection));
                OnPropertyChanged(nameof(MultiCollection));
                OnPropertyChanged(nameof(MediaItem));
                OnPropertyChanged(nameof(SmartCollection));
                OnPropertyChanged(nameof(SearchTitle));
                OnPropertyChanged(nameof(SearchQuery));
                OnPropertyChanged(nameof(RerunCollection));
                OnPropertyChanged(nameof(MultiCollection));
                OnPropertyChanged(nameof(PlaybackOrder));
            }

            if (_collectionType == CollectionType.MultiCollection)
            {
                PlaybackOrder = PlaybackOrder.Shuffle;
            }
        }
    }

    public MediaCollectionViewModel Collection { get; set; }
    public MultiCollectionViewModel MultiCollection { get; set; }
    public SmartCollectionViewModel SmartCollection { get; set; }
    public RerunCollectionViewModel RerunCollection { get; set; }
    public NamedMediaItemViewModel MediaItem { get; set; }
    public PlaylistViewModel Playlist { get; set; }
    public string SearchTitle { get; set; }
    public string SearchQuery { get; set; }
    public FillerPresetViewModel PreRollFiller { get; set; }
    public FillerPresetViewModel MidRollFiller { get; set; }
    public FillerPresetViewModel PostRollFiller { get; set; }
    public FillerPresetViewModel TailFiller { get; set; }
    public FillerPresetViewModel FallbackFiller { get; set; }
    public IEnumerable<WatermarkViewModel> Watermarks { get; set; }
    public IEnumerable<GraphicsElementViewModel> GraphicsElements { get; set; }
    public string PreferredAudioLanguageCode { get; set; }
    public string PreferredAudioTitle { get; set; }
    public string PreferredSubtitleLanguageCode { get; set; }
    public ChannelSubtitleMode? SubtitleMode { get; set; }

    public string CollectionName => CollectionType switch
    {
        CollectionType.Collection => Collection?.Name,
        CollectionType.TelevisionShow => MediaItem?.Name,
        CollectionType.TelevisionSeason => MediaItem?.Name,
        CollectionType.Artist => MediaItem?.Name,
        CollectionType.MultiCollection => MultiCollection?.Name,
        CollectionType.SmartCollection => SmartCollection?.Name,
        CollectionType.Playlist => Playlist?.Name,
        CollectionType.RerunFirstRun or CollectionType.RerunRerun => RerunCollection?.Name,
        CollectionType.SearchQuery => string.IsNullOrWhiteSpace(SearchTitle) ? SearchQuery : SearchTitle,
        _ => string.Empty
    };

    public PlaybackOrder PlaybackOrder
    {
        get => _playbackOrder;
        set
        {
            if (value == _playbackOrder)
            {
                return;
            }

            _playbackOrder = value;

            if (_playbackOrder is not PlaybackOrder.Chronological && MultipleMode is MultipleMode.MultiEpisodeGroupSize)
            {
                MultipleMode = MultipleMode.Count;
            }

            if (_playbackOrder is not PlaybackOrder.Marathon)
            {
                MarathonGroupBy = MarathonGroupBy.None;
                MarathonShuffleItems = false;
                MarathonBatchSize = null;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(CanFillWithGroups));
            OnPropertyChanged(nameof(MultipleMode));
        }
    }

    public MarathonGroupBy MarathonGroupBy { get; set; }

    public bool MarathonShuffleGroups { get; set; }

    public bool MarathonShuffleItems { get; set; }

    public int? MarathonBatchSize { get; set; }

    public MultipleMode MultipleMode { get; set; }

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
