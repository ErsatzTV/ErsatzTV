using System.Collections.Generic;

namespace ErsatzTV.Application.Playouts;

public record PagedPlayoutItemsViewModel(int TotalCount, List<PlayoutItemViewModel> Page);