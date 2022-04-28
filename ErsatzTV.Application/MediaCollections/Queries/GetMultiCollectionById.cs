namespace ErsatzTV.Application.MediaCollections;

public record GetMultiCollectionById(int Id) : IRequest<Option<MultiCollectionViewModel>>;
