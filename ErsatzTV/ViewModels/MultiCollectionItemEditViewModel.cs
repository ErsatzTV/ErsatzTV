using ErsatzTV.Application.MediaCollections;

namespace ErsatzTV.ViewModels
{
    public class MultiCollectionItemEditViewModel
    {
        public MediaCollectionViewModel Collection { get; set; }
        public bool ScheduleAsGroup { get; set; }
    }
}
