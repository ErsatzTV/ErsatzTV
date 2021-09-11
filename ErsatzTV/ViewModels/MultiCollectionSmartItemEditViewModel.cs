using ErsatzTV.Application.MediaCollections;

namespace ErsatzTV.ViewModels
{
    public class MultiCollectionSmartItemEditViewModel : MultiCollectionItemEditViewModel
    {
        private SmartCollectionViewModel _smartCollection;

        public SmartCollectionViewModel SmartCollection
        {
            get => _smartCollection;
            set
            {
                _smartCollection = value;

                Collection = new MediaCollectionViewModel(
                    _smartCollection.Id,
                    _smartCollection.Name,
                    false);
            }
        }

        public override MediaCollectionViewModel Collection { get; set; }
    }
}
