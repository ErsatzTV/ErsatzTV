namespace ErsatzTV.Application.MediaCollections;

public record GetRerunCollectionById(int Id) : IRequest<Option<RerunCollectionViewModel>>;
