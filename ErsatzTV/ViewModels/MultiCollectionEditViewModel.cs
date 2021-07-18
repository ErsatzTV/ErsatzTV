using System.Collections.Generic;

namespace ErsatzTV.ViewModels
{
    public class MultiCollectionEditViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<MultiCollectionItemEditViewModel> Items { get; set; }
    }
}
