using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace ErsatzTV.Application.Jellyfin;

public class GetJellyfinConnectionParametersHandler : IRequestHandler<GetJellyfinConnectionParameters,
    Either<BaseError, JellyfinConnectionParametersViewModel>>
{
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IMemoryCache _memoryCache;

    public GetJellyfinConnectionParametersHandler(
        IMemoryCache memoryCache,
        IMediaSourceRepository mediaSourceRepository)
    {
        _memoryCache = memoryCache;
        _mediaSourceRepository = mediaSourceRepository;
    }

    public async Task<Either<BaseError, JellyfinConnectionParametersViewModel>> Handle(
        GetJellyfinConnectionParameters request,
        CancellationToken cancellationToken)
    {
        if (_memoryCache.TryGetValue(request, out JellyfinConnectionParametersViewModel parameters))
        {
            return parameters;
        }

        Either<BaseError, JellyfinConnectionParametersViewModel> maybeParameters =
            await Validate()
                .MapT(cp => new JellyfinConnectionParametersViewModel(cp.ActiveConnection.Address))
                .Map(v => v.ToEither<JellyfinConnectionParametersViewModel>());

        return maybeParameters.Match(
            p =>
            {
                _memoryCache.Set(request, p, TimeSpan.FromHours(1));
                return maybeParameters;
            },
            error => error);
    }

    private Task<Validation<BaseError, ConnectionParameters>> Validate() =>
        JellyfinMediaSourceMustExist()
            .BindT(MediaSourceMustHaveActiveConnection);

    private Task<Validation<BaseError, JellyfinMediaSource>> JellyfinMediaSourceMustExist() =>
        _mediaSourceRepository.GetAllJellyfin().Map(list => list.HeadOrNone())
            .Map(
                v => v.ToValidation<BaseError>(
                    "Jellyfin media source does not exist."));

    private Validation<BaseError, ConnectionParameters> MediaSourceMustHaveActiveConnection(
        JellyfinMediaSource jellyfinMediaSource)
    {
        Option<JellyfinConnection> maybeConnection = jellyfinMediaSource.Connections.FirstOrDefault();
        return maybeConnection.Map(connection => new ConnectionParameters(jellyfinMediaSource, connection))
            .ToValidation<BaseError>("Jellyfin media source requires an active connection");
    }

    private record ConnectionParameters(
        JellyfinMediaSource JellyfinMediaSource,
        JellyfinConnection ActiveConnection);
}
