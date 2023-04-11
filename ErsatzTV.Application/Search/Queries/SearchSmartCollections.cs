using ErsatzTV.Application.MediaCollections;

namespace ErsatzTV.Application.Search;

public record SearchSmartCollections(string Query) : IRequest<List<SmartCollectionViewModel>>;
