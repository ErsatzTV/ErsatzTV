using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace ErsatzTV.Application.Emby;

public class GetEmbyConnectionParametersHandler : IRequestHandler<GetEmbyConnectionParameters,
    Either<BaseError, EmbyConnectionParametersViewModel>>
{
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IMemoryCache _memoryCache;

    public GetEmbyConnectionParametersHandler(
        IMemoryCache memoryCache,
        IMediaSourceRepository mediaSourceRepository)
    {
        _memoryCache = memoryCache;
        _mediaSourceRepository = mediaSourceRepository;
    }

    public async Task<Either<BaseError, EmbyConnectionParametersViewModel>> Handle(
        GetEmbyConnectionParameters request,
        CancellationToken cancellationToken)
    {
        if (_memoryCache.TryGetValue(request, out EmbyConnectionParametersViewModel parameters))
        {
            return parameters;
        }

        Either<BaseError, EmbyConnectionParametersViewModel> maybeParameters =
            await Validate()
                .MapT(cp => new EmbyConnectionParametersViewModel(cp.ActiveConnection.Address))
                .Map(v => v.ToEither<EmbyConnectionParametersViewModel>());

        return maybeParameters.Match(
            p =>
            {
                _memoryCache.Set(request, p, TimeSpan.FromHours(1));
                return maybeParameters;
            },
            error => error);
    }

    private Task<Validation<BaseError, ConnectionParameters>> Validate() =>
        EmbyMediaSourceMustExist()
            .BindT(MediaSourceMustHaveActiveConnection);

    private Task<Validation<BaseError, EmbyMediaSource>> EmbyMediaSourceMustExist() =>
        _mediaSourceRepository.GetAllEmby().Map(list => list.HeadOrNone())
            .Map(
                v => v.ToValidation<BaseError>(
                    "Emby media source does not exist."));

    private Validation<BaseError, ConnectionParameters> MediaSourceMustHaveActiveConnection(
        EmbyMediaSource embyMediaSource)
    {
        Option<EmbyConnection> maybeConnection = embyMediaSource.Connections.FirstOrDefault();
        return maybeConnection.Map(connection => new ConnectionParameters(embyMediaSource, connection))
            .ToValidation<BaseError>("Emby media source requires an active connection");
    }

    private record ConnectionParameters(
        EmbyMediaSource EmbyMediaSource,
        EmbyConnection ActiveConnection);
}