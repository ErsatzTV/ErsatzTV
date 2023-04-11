using ErsatzTV.Application.MediaCollections;

namespace ErsatzTV.Application.Search;

public record SearchCollections(string Query) : IRequest<List<MediaCollectionViewModel>>;
