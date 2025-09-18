using System.ComponentModel;
using System.Runtime.CompilerServices;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.ViewModels;

public class DecoBreakContentEditViewModel : INotifyPropertyChanged
{
    private CollectionType _collectionType;

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
                Playlist = null;

                OnPropertyChanged(nameof(Collection));
                OnPropertyChanged(nameof(MultiCollection));
                OnPropertyChanged(nameof(MediaItem));
                OnPropertyChanged(nameof(SmartCollection));
                OnPropertyChanged(nameof(Playlist));
            }
        }
    }

    public MediaCollectionViewModel Collection { get; set; }
    public MultiCollectionViewModel MultiCollection { get; set; }
    public SmartCollectionViewModel SmartCollection { get; set; }
    public NamedMediaItemViewModel MediaItem { get; set; }
    public PlaylistViewModel Playlist { get; set; }

    public string CollectionName => CollectionType switch
    {
        CollectionType.Collection => Collection?.Name,
        CollectionType.TelevisionShow => MediaItem?.Name,
        CollectionType.TelevisionSeason => MediaItem?.Name,
        CollectionType.Artist => MediaItem?.Name,
        CollectionType.MultiCollection => MultiCollection?.Name,
        CollectionType.SmartCollection => SmartCollection?.Name,
        CollectionType.Playlist => Playlist?.Name,
        _ => string.Empty
    };

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
