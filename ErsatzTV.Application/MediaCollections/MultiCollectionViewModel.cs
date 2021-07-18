using System.Collections.Generic;

namespace ErsatzTV.Application.MediaCollections
{
    public record MultiCollectionViewModel(int Id, string Name, List<MultiCollectionItemViewModel> Items);
}
