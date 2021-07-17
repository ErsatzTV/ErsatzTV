using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ErsatzTV.Annotations;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.ViewModels
{
    public class ProgramScheduleItemEditViewModel : INotifyPropertyChanged
    {
        private ProgramScheduleItemCollectionType _collectionType;
        private int? _multipleCount;
        private bool? _offlineTail;
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

                    OnPropertyChanged(nameof(Collection));
                    OnPropertyChanged(nameof(MultiCollection));
                    OnPropertyChanged(nameof(MediaItem));
                }
            }
        }

        public MediaCollectionViewModel Collection { get; set; }
        public MultiCollectionViewModel MultiCollection { get; set; }
        public NamedMediaItemViewModel MediaItem { get; set; }

        public string CollectionName => CollectionType switch
        {
            ProgramScheduleItemCollectionType.Collection => Collection?.Name,
            ProgramScheduleItemCollectionType.TelevisionShow => MediaItem?.Name,
            ProgramScheduleItemCollectionType.TelevisionSeason => MediaItem?.Name,
            ProgramScheduleItemCollectionType.Artist => MediaItem?.Name,
            ProgramScheduleItemCollectionType.MultiCollection => MultiCollection?.Name,
            _ => string.Empty
        };

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

        public bool? OfflineTail
        {
            get => PlayoutMode == PlayoutMode.Duration ? _offlineTail : null;
            set => _offlineTail = value;
        }

        public string CustomTitle { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(
            [CallerMemberName]
            string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
