using System.Collections.Generic;

namespace ErsatzTV.Application.MediaCollections;

public record PagedSmartCollectionsViewModel(int TotalCount, List<SmartCollectionViewModel> Page);