using System;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.ViewModels
{
    public class ProgramScheduleItemEditViewModel
    {
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
        public MediaCollectionViewModel MediaCollection { get; set; }

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
    }
}
