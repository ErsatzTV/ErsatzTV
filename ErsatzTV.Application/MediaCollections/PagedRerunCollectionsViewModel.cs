namespace ErsatzTV.Application.MediaCollections;

public record PagedRerunCollectionsViewModel(int TotalCount, List<RerunCollectionViewModel> Page);
