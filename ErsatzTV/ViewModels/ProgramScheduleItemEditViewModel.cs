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
    private int? _multipleCount;
    private TimeSpan? _playoutDuration;
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
        set => _playoutDuration = value;
    }

    public TailMode TailMode { get; set; }

    public string CustomTitle { get; set; }

    public GuideMode GuideMode { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged(
        [CallerMemberName]
        string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
