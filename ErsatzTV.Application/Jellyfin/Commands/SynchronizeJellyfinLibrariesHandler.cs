using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Jellyfin;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Jellyfin;

public class
    SynchronizeJellyfinLibrariesHandler : IRequestHandler<SynchronizeJellyfinLibraries, Either<BaseError, Unit>>
{
    private readonly IJellyfinApiClient _jellyfinApiClient;
    private readonly IJellyfinSecretStore _jellyfinSecretStore;
    private readonly ILogger<SynchronizeJellyfinLibrariesHandler> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly ISearchIndex _searchIndex;

    public SynchronizeJellyfinLibrariesHandler(
        IMediaSourceRepository mediaSourceRepository,
        IJellyfinSecretStore jellyfinSecretStore,
        IJellyfinApiClient jellyfinApiClient,
        ILogger<SynchronizeJellyfinLibrariesHandler> logger,
        ISearchIndex searchIndex)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _jellyfinSecretStore = jellyfinSecretStore;
        _jellyfinApiClient = jellyfinApiClient;
        _logger = logger;
        _searchIndex = searchIndex;
    }

    public Task<Either<BaseError, Unit>> Handle(
        SynchronizeJellyfinLibraries request,
        CancellationToken cancellationToken) =>
        Validate(request)
            .MapT(p => SynchronizeLibraries(p, cancellationToken))
            .Bind(v => v.ToEitherAsync());

    private Task<Validation<BaseError, ConnectionAndSource>> Validate(SynchronizeJellyfinLibraries request) =>
        MediaSourceMustExist(request)
            .BindT(MediaSourceMustHaveActiveConnection)
            .BindT(MediaSourceMustHaveApiKey);

    private Task<Validation<BaseError, JellyfinMediaSource>> MediaSourceMustExist(
        SynchronizeJellyfinLibraries request) =>
        _mediaSourceRepository.GetJellyfin(request.JellyfinMediaSourceId)
            .Map(o => o.ToValidation<BaseError>("Jellyfin media source does not exist."));

    private Validation<BaseError, ConnectionAndSource> MediaSourceMustHaveActiveConnection(
        JellyfinMediaSource jellyfinMediaSource)
    {
        Option<JellyfinConnection> maybeConnection = jellyfinMediaSource.Connections.HeadOrNone();
        return maybeConnection.Map(connection => new ConnectionAndSource(
                new JellyfinConnectionParameters(connection.Address, string.Empty, connection.JellyfinMediaSourceId),
                jellyfinMediaSource))
            .ToValidation<BaseError>("Jellyfin media source requires an active connection");
    }

    private async Task<Validation<BaseError, ConnectionAndSource>> MediaSourceMustHaveApiKey(
        ConnectionAndSource connectionAndSource)
    {
        JellyfinSecrets secrets = await _jellyfinSecretStore.ReadSecrets();
        return Optional(secrets.Address == connectionAndSource.ConnectionParameters.Address)
            .Where(match => match)
            .Map(_ => connectionAndSource with
            {
                ConnectionParameters = connectionAndSource.ConnectionParameters with { ApiKey = secrets.ApiKey }
            })
            .ToValidation<BaseError>("Jellyfin media source requires an api key");
    }

    private async Task<Unit> SynchronizeLibraries(
        ConnectionAndSource connectionAndSource,
        CancellationToken cancellationToken)
    {
        Either<BaseError, List<JellyfinLibrary>> maybeLibraries = await _jellyfinApiClient.GetLibraries(
            connectionAndSource.ConnectionParameters.Address,
            connectionAndSource.ConnectionParameters.AuthorizationHeader);

        foreach (BaseError error in maybeLibraries.LeftToSeq())
        {
            _logger.LogWarning(
                "Unable to synchronize libraries from jellyfin server {JellyfinServer}: {Error}",
                connectionAndSource.MediaSource.ServerName,
                error.Value);
        }

        foreach (List<JellyfinLibrary> libraries in maybeLibraries.RightToSeq())
        {
            var existing = connectionAndSource.MediaSource.Libraries
                .OfType<JellyfinLibrary>()
                .ToList();
            var toAdd = libraries.Filter(library => existing.All(l => l.ItemId != library.ItemId)).ToList();
            var toRemove = existing.Filter(library => libraries.All(l => l.ItemId != library.ItemId)).ToList();
            var toUpdate = libraries
                .Filter(l => toAdd.All(a => a.ItemId != l.ItemId) && toRemove.All(r => r.ItemId != l.ItemId)).ToList();
            List<int> ids = await _mediaSourceRepository.UpdateLibraries(
                connectionAndSource.MediaSource.Id,
                toAdd,
                toRemove,
                toUpdate,
                cancellationToken);
            if (ids.Count != 0)
            {
                await _searchIndex.RemoveItems(ids);
                _searchIndex.Commit();
            }
        }

        return Unit.Default;
    }

    private sealed record ConnectionAndSource(
        JellyfinConnectionParameters ConnectionParameters,
        JellyfinMediaSource MediaSource);
}
