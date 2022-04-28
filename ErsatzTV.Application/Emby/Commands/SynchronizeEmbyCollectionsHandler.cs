using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.Emby;

public class SynchronizeEmbyCollectionsHandler : IRequestHandler<SynchronizeEmbyCollections, Either<BaseError, Unit>>
{
    private readonly IEmbySecretStore _embySecretStore;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IEmbyCollectionScanner _scanner;

    public SynchronizeEmbyCollectionsHandler(
        IMediaSourceRepository mediaSourceRepository,
        IEmbySecretStore embySecretStore,
        IEmbyCollectionScanner scanner)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _embySecretStore = embySecretStore;
        _scanner = scanner;
    }


    public async Task<Either<BaseError, Unit>> Handle(
        SynchronizeEmbyCollections request,
        CancellationToken cancellationToken)
    {
        Validation<BaseError, ConnectionParameters> validation = await Validate(request);
        return await validation.Match(
            SynchronizeCollections,
            error => Task.FromResult<Either<BaseError, Unit>>(error.Join()));
    }

    private Task<Validation<BaseError, ConnectionParameters>> Validate(SynchronizeEmbyCollections request) =>
        MediaSourceMustExist(request)
            .BindT(MediaSourceMustHaveActiveConnection)
            .BindT(MediaSourceMustHaveApiKey);

    private Task<Validation<BaseError, EmbyMediaSource>> MediaSourceMustExist(
        SynchronizeEmbyCollections request) =>
        _mediaSourceRepository.GetEmby(request.EmbyMediaSourceId)
            .Map(o => o.ToValidation<BaseError>("Emby media source does not exist."));

    private Validation<BaseError, ConnectionParameters> MediaSourceMustHaveActiveConnection(
        EmbyMediaSource embyMediaSource)
    {
        Option<EmbyConnection> maybeConnection = embyMediaSource.Connections.HeadOrNone();
        return maybeConnection.Map(connection => new ConnectionParameters(connection))
            .ToValidation<BaseError>("Emby media source requires an active connection");
    }

    private async Task<Validation<BaseError, ConnectionParameters>> MediaSourceMustHaveApiKey(
        ConnectionParameters connectionParameters)
    {
        EmbySecrets secrets = await _embySecretStore.ReadSecrets();
        return Optional(secrets.Address == connectionParameters.ActiveConnection.Address)
            .Where(match => match)
            .Map(_ => connectionParameters with { ApiKey = secrets.ApiKey })
            .ToValidation<BaseError>("Emby media source requires an api key");
    }

    private async Task<Either<BaseError, Unit>> SynchronizeCollections(ConnectionParameters connectionParameters) =>
        await _scanner.ScanCollections(
            connectionParameters.ActiveConnection.Address,
            connectionParameters.ApiKey);

    private record ConnectionParameters(EmbyConnection ActiveConnection)
    {
        public string ApiKey { get; set; }
    }
}
