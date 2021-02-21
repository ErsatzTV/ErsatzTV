using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ErsatzTV.Annotations;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.Television;
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
                _collectionType = value;

                switch (CollectionType)
                {
                    case ProgramScheduleItemCollectionType.Collection:
                        TelevisionShow = null;
                        TelevisionSeason = null;
                        break;
                    case ProgramScheduleItemCollectionType.TelevisionShow:
                        MediaCollection = null;
                        TelevisionSeason = null;
                        break;
                    case ProgramScheduleItemCollectionType.TelevisionSeason:
                        MediaCollection = null;
                        TelevisionShow = null;
                        break;
                }

                OnPropertyChanged(nameof(MediaCollection));
                OnPropertyChanged(nameof(TelevisionShow));
                OnPropertyChanged(nameof(TelevisionSeason));
            }
        }

        public MediaCollectionViewModel MediaCollection { get; set; }
        public TelevisionShowViewModel TelevisionShow { get; set; }
        public TelevisionSeasonViewModel TelevisionSeason { get; set; }

        public string CollectionName { get; set; }

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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(
            [CallerMemberName]
            string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
