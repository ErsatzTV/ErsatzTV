﻿namespace ErsatzTV.Application.MediaCollections;

public record PagedMediaCollectionsViewModel(int TotalCount, List<MediaCollectionViewModel> Page);
