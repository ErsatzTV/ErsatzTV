namespace ErsatzTV.Application.MediaCollections;

public record GetCollectionById(int Id) : IRequest<Option<MediaCollectionViewModel>>;