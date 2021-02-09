using System;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.ViewModels
{
    public class ProgramScheduleItemEditViewModel
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public StartType StartType { get; set; }
        public TimeSpan? StartTime { get; set; }
        public PlayoutMode PlayoutMode { get; set; }
        public string MediaCollectionName { get; set; }
    }
}
