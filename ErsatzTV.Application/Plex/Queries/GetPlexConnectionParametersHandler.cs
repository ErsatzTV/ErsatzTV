using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Plex;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace ErsatzTV.Application.Plex;

public class GetPlexConnectionParametersHandler : IRequestHandler<GetPlexConnectionParameters,
    Either<BaseError, PlexConnectionParametersViewModel>>
{
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly IPlexSecretStore _plexSecretStore;

    public GetPlexConnectionParametersHandler(
        IMemoryCache memoryCache,
        IMediaSourceRepository mediaSourceRepository,
        IPlexSecretStore plexSecretStore)
    {
        _memoryCache = memoryCache;
        _mediaSourceRepository = mediaSourceRepository;
        _plexSecretStore = plexSecretStore;
    }

    public async Task<Either<BaseError, PlexConnectionParametersViewModel>> Handle(
        GetPlexConnectionParameters request,
        CancellationToken cancellationToken)
    {
        if (_memoryCache.TryGetValue(request, out PlexConnectionParametersViewModel parameters))
        {
            return parameters;
        }

        Either<BaseError, PlexConnectionParametersViewModel> maybeParameters =
            await Validate(request)
                .MapT(
                    cp => new PlexConnectionParametersViewModel(
                        new Uri(cp.ActiveConnection.Uri),
                        cp.PlexServerAuthToken.AuthToken))
                .Map(v => v.ToEither<PlexConnectionParametersViewModel>());

        return maybeParameters.Match(
            p =>
            {
                _memoryCache.Set(request, p, TimeSpan.FromHours(1));
                return maybeParameters;
            },
            error => error);
    }

    private Task<Validation<BaseError, ConnectionParameters>> Validate(GetPlexConnectionParameters request) =>
        PlexMediaSourceMustExist(request)
            .BindT(MediaSourceMustHaveActiveConnection)
            .BindT(MediaSourceMustHaveToken);

    private Task<Validation<BaseError, PlexMediaSource>> PlexMediaSourceMustExist(
        GetPlexConnectionParameters request) =>
        _mediaSourceRepository.GetPlex(request.PlexMediaSourceId)
            .Map(
                v => v.ToValidation<BaseError>(
                    $"Plex media source {request.PlexMediaSourceId} does not exist."));

    private Validation<BaseError, ConnectionParameters> MediaSourceMustHaveActiveConnection(
        PlexMediaSource plexMediaSource)
    {
        Option<PlexConnection> maybeConnection =
            plexMediaSource.Connections.SingleOrDefault(c => c.IsActive);
        return maybeConnection.Map(connection => new ConnectionParameters(plexMediaSource, connection))
            .ToValidation<BaseError>("Plex media source requires an active connection");
    }

    private async Task<Validation<BaseError, ConnectionParameters>> MediaSourceMustHaveToken(
        ConnectionParameters connectionParameters)
    {
        Option<PlexServerAuthToken> maybeToken = await
            _plexSecretStore.GetServerAuthToken(connectionParameters.PlexMediaSource.ClientIdentifier);
        return maybeToken.Map(token => connectionParameters with { PlexServerAuthToken = token })
            .ToValidation<BaseError>("Plex media source requires a token");
    }

    private record ConnectionParameters(PlexMediaSource PlexMediaSource, PlexConnection ActiveConnection)
    {
        public PlexServerAuthToken PlexServerAuthToken { get; set; }
    }
}