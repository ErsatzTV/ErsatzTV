namespace ErsatzTV.Application.MediaItems;

public record GetRemoteStreamById(int RemoteStreamId) : IRequest<Option<RemoteStreamViewModel>>;
