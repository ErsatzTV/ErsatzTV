using System.ComponentModel;
using System.Runtime.CompilerServices;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.ViewModels;

public class PlaylistItemEditViewModel : INotifyPropertyChanged
{
    private ProgramScheduleItemCollectionType _collectionType;

    public int Id { get; set; }
    public int Index { get; set; }

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

    public string ItemType => CollectionType switch
    {
        ProgramScheduleItemCollectionType.Collection => "Collection",
        ProgramScheduleItemCollectionType.TelevisionShow => "Show",
        ProgramScheduleItemCollectionType.TelevisionSeason => "Season",
        ProgramScheduleItemCollectionType.Artist => "Artist",
        ProgramScheduleItemCollectionType.MultiCollection => "Multi-Collection",
        ProgramScheduleItemCollectionType.SmartCollection => "Smart Collection",
        ProgramScheduleItemCollectionType.Movie => "Movie",
        ProgramScheduleItemCollectionType.Episode => "Episode",
        ProgramScheduleItemCollectionType.MusicVideo => "Music Video",
        ProgramScheduleItemCollectionType.OtherVideo => "Other Video",
        ProgramScheduleItemCollectionType.Song => "Song",
        ProgramScheduleItemCollectionType.Image => "Image",
        _ => string.Empty
    };

    public string ItemName => CollectionType switch
    {
        ProgramScheduleItemCollectionType.Collection => Collection?.Name,
        ProgramScheduleItemCollectionType.TelevisionShow => MediaItem?.Name,
        ProgramScheduleItemCollectionType.TelevisionSeason => MediaItem?.Name,
        ProgramScheduleItemCollectionType.Artist => MediaItem?.Name,
        ProgramScheduleItemCollectionType.MultiCollection => MultiCollection?.Name,
        ProgramScheduleItemCollectionType.SmartCollection => SmartCollection?.Name,
        ProgramScheduleItemCollectionType.Movie => MediaItem?.Name,
        ProgramScheduleItemCollectionType.Episode => MediaItem?.Name,
        ProgramScheduleItemCollectionType.MusicVideo => MediaItem?.Name,
        ProgramScheduleItemCollectionType.OtherVideo => MediaItem?.Name,
        ProgramScheduleItemCollectionType.Song => MediaItem?.Name,
        ProgramScheduleItemCollectionType.Image => MediaItem?.Name,
        _ => string.Empty
    };

    public PlaybackOrder PlaybackOrder { get; set; }

    public bool PlayAll { get; set; }

    public bool IncludeInProgramGuide { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
