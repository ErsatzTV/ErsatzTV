using System.ComponentModel;
using System.Runtime.CompilerServices;
using ErsatzTV.Application.Graphics;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Application.Watermarks;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.ViewModels;

public class BlockItemEditViewModel : INotifyPropertyChanged
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
                SearchTitle = null;
                SearchQuery = null;

                OnPropertyChanged(nameof(Collection));
                OnPropertyChanged(nameof(MultiCollection));
                OnPropertyChanged(nameof(MediaItem));
                OnPropertyChanged(nameof(SmartCollection));
                OnPropertyChanged(nameof(SearchTitle));
                OnPropertyChanged(nameof(SearchQuery));
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
    public string SearchTitle { get; set; }
    public string SearchQuery { get; set; }

    public string CollectionName => CollectionType switch
    {
        CollectionType.Collection => Collection?.Name,
        CollectionType.TelevisionShow => MediaItem?.Name,
        CollectionType.TelevisionSeason => MediaItem?.Name,
        CollectionType.Artist => MediaItem?.Name,
        CollectionType.MultiCollection => MultiCollection?.Name,
        CollectionType.SmartCollection => SmartCollection?.Name,
        CollectionType.SearchQuery => string.IsNullOrWhiteSpace(SearchTitle) ? SearchQuery : SearchTitle,
        _ => string.Empty
    };

    public PlaybackOrder PlaybackOrder { get; set; }

    public bool IncludeInProgramGuide { get; set; }

    public bool DisableWatermarks { get; set; }

    public IEnumerable<WatermarkViewModel> Watermarks { get; set; }

    public IEnumerable<GraphicsElementViewModel> GraphicsElements { get; set; }

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
