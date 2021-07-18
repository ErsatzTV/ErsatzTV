using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.ViewModels
{
    public class MultiCollectionItemEditViewModel
    {
        public MediaCollectionViewModel Collection { get; set; }
        public bool ScheduleAsGroup { get; set; }
        public PlaybackOrder PlaybackOrder { get; set; }
    }
}
