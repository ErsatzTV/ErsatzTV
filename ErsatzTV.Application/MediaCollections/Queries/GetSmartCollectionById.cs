namespace ErsatzTV.Application.MediaCollections;

public record GetSmartCollectionById(int Id) : IRequest<Option<SmartCollectionViewModel>>;
