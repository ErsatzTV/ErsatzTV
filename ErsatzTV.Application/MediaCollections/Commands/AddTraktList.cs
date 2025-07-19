using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record AddTraktList(string TraktListUrl, string User, string List, bool Unlock)
    : IRequest<Either<BaseError, Unit>>, IBackgroundServiceRequest
{
    public static AddTraktList FromUrl(string traktListUrl) => new(traktListUrl, string.Empty, string.Empty, true);
    public static AddTraktList Existing(string user, string list, bool unlock) => new(string.Empty, user, list, unlock);
}
