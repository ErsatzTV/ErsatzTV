using ErsatzTV.Application.MediaCollections;

namespace ErsatzTV.Application.Search;

public record SearchMultiCollections(string Query) : IRequest<List<MultiCollectionViewModel>>;
