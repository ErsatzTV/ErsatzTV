using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record AddTraktList(string TraktListUrl) : IRequest<Either<BaseError, Unit>>, IBackgroundServiceRequest;
