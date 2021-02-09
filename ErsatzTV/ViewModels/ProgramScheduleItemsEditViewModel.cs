using System.Collections.Generic;

namespace ErsatzTV.ViewModels
{
    public class ProgramScheduleItemsEditViewModel
    {
        public string Name { get; set; }
        public List<ProgramScheduleItemEditViewModel> Items { get; set; }
    }
}
