using ErsatzTV.Application.MediaCollections;

namespace ErsatzTV.Application.Search;

public record SearchRerunCollections(string Query) : IRequest<List<RerunCollectionViewModel>>;
