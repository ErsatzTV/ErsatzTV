using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Jellyfin;

namespace ErsatzTV.Application.Jellyfin;

public class SynchronizeJellyfinCollectionsHandler :
    IRequestHandler<SynchronizeJellyfinCollections, Either<BaseError, Unit>>
{
    private readonly IJellyfinSecretStore _jellyfinSecretStore;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IJellyfinCollectionScanner _scanner;

    public SynchronizeJellyfinCollectionsHandler(
        IMediaSourceRepository mediaSourceRepository,
        IJellyfinSecretStore jellyfinSecretStore,
        IJellyfinCollectionScanner scanner)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _jellyfinSecretStore = jellyfinSecretStore;
        _scanner = scanner;
    }


    public async Task<Either<BaseError, Unit>> Handle(
        SynchronizeJellyfinCollections request,
        CancellationToken cancellationToken)
    {
        Validation<BaseError, ConnectionParameters> validation = await Validate(request);
        return await validation.Match(
            SynchronizeCollections,
            error => Task.FromResult<Either<BaseError, Unit>>(error.Join()));
    }

    private Task<Validation<BaseError, ConnectionParameters>> Validate(SynchronizeJellyfinCollections request) =>
        MediaSourceMustExist(request)
            .BindT(MediaSourceMustHaveActiveConnection)
            .BindT(MediaSourceMustHaveApiKey);

    private Task<Validation<BaseError, JellyfinMediaSource>> MediaSourceMustExist(
        SynchronizeJellyfinCollections request) =>
        _mediaSourceRepository.GetJellyfin(request.JellyfinMediaSourceId)
            .Map(o => o.ToValidation<BaseError>("Jellyfin media source does not exist."));

    private Validation<BaseError, ConnectionParameters> MediaSourceMustHaveActiveConnection(
        JellyfinMediaSource jellyfinMediaSource)
    {
        Option<JellyfinConnection> maybeConnection = jellyfinMediaSource.Connections.HeadOrNone();
        return maybeConnection.Map(connection => new ConnectionParameters(jellyfinMediaSource, connection))
            .ToValidation<BaseError>("Jellyfin media source requires an active connection");
    }

    private async Task<Validation<BaseError, ConnectionParameters>> MediaSourceMustHaveApiKey(
        ConnectionParameters connectionParameters)
    {
        JellyfinSecrets secrets = await _jellyfinSecretStore.ReadSecrets();
        return Optional(secrets.Address == connectionParameters.ActiveConnection.Address)
            .Where(match => match)
            .Map(_ => connectionParameters with { ApiKey = secrets.ApiKey })
            .ToValidation<BaseError>("Jellyfin media source requires an api key");
    }

    private async Task<Either<BaseError, Unit>> SynchronizeCollections(ConnectionParameters connectionParameters) =>
        await _scanner.ScanCollections(
            connectionParameters.ActiveConnection.Address,
            connectionParameters.ApiKey,
            connectionParameters.JellyfinMediaSource.Id);

    private record ConnectionParameters(
        JellyfinMediaSource JellyfinMediaSource,
        JellyfinConnection ActiveConnection)
    {
        public string ApiKey { get; set; }
    }
}
