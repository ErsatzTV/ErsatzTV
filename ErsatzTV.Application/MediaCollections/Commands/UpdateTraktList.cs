using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record UpdateTraktList(int Id, bool AutoRefresh, bool GeneratePlaylist) : IRequest<Option<BaseError>>;
