namespace ErsatzTV.Application.MediaCollections;

public record GetTraktListById(int Id) : IRequest<Option<TraktListViewModel>>;
