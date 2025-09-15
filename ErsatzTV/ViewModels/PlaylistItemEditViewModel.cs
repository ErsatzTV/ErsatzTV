using System.ComponentModel;
using System.Runtime.CompilerServices;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.ViewModels;

public class PlaylistItemEditViewModel : INotifyPropertyChanged
{
    private CollectionType _collectionType;
    private int? _count;
    private bool _playAll;

    public int Id { get; set; }
    public int Index { get; set; }

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

                OnPropertyChanged(nameof(Collection));
                OnPropertyChanged(nameof(MultiCollection));
                OnPropertyChanged(nameof(MediaItem));
                OnPropertyChanged(nameof(SmartCollection));
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
    public NamedMediaItemViewModel MediaItem { get; set; }

    public string ItemType => CollectionType switch
    {
        CollectionType.Collection => "Collection",
        CollectionType.TelevisionShow => "Show",
        CollectionType.TelevisionSeason => "Season",
        CollectionType.Artist => "Artist",
        CollectionType.MultiCollection => "Multi-Collection",
        CollectionType.SmartCollection => "Smart Collection",
        CollectionType.Movie => "Movie",
        CollectionType.Episode => "Episode",
        CollectionType.MusicVideo => "Music Video",
        CollectionType.OtherVideo => "Other Video",
        CollectionType.Song => "Song",
        CollectionType.Image => "Image",
        _ => string.Empty
    };

    public string ItemName => CollectionType switch
    {
        CollectionType.Collection => Collection?.Name,
        CollectionType.TelevisionShow => MediaItem?.Name,
        CollectionType.TelevisionSeason => MediaItem?.Name,
        CollectionType.Artist => MediaItem?.Name,
        CollectionType.MultiCollection => MultiCollection?.Name,
        CollectionType.SmartCollection => SmartCollection?.Name,
        CollectionType.Movie => MediaItem?.Name,
        CollectionType.Episode => MediaItem?.Name,
        CollectionType.MusicVideo => MediaItem?.Name,
        CollectionType.OtherVideo => MediaItem?.Name,
        CollectionType.Song => MediaItem?.Name,
        CollectionType.Image => MediaItem?.Name,
        _ => string.Empty
    };

    public PlaybackOrder PlaybackOrder { get; set; }

    public int? Count
    {
        get => _count;
        set
        {
            if (value == _count)
            {
                return;
            }

            _count = value;
            OnPropertyChanged();

            if (_count is not null)
            {
                _playAll = false;
                OnPropertyChanged(nameof(PlayAll));
            }
        }
    }

    public bool PlayAll
    {
        get => _playAll;
        set
        {
            if (value == _playAll)
            {
                return;
            }

            _playAll = value;
            OnPropertyChanged();

            if (_playAll)
            {
                _count = null;
            }
            else
            {
                _count ??= 1;
            }
            OnPropertyChanged(nameof(Count));
        }
    }

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
