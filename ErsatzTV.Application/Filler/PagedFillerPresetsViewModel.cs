using System.Collections.Generic;

namespace ErsatzTV.Application.Filler
{
    public record PagedFillerPresetsViewModel(int TotalCount, List<FillerPresetViewModel> Page);
}
