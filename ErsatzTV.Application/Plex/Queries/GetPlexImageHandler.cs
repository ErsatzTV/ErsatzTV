using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Application.Images;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Plex;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Plex.Queries
{
    public class GetPlexImageHandler : IRequestHandler<GetPlexImage, Either<BaseError, ImageViewModel>>
    {
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly IMemoryCache _memoryCache;
        private readonly IPlexSecretStore _plexSecretStore;

        public GetPlexImageHandler(
            IMemoryCache memoryCache,
            IMediaSourceRepository mediaSourceRepository,
            IPlexSecretStore plexSecretStore)
        {
            _memoryCache = memoryCache;
            _mediaSourceRepository = mediaSourceRepository;
            _plexSecretStore = plexSecretStore;
        }

        public async Task<Either<BaseError, ImageViewModel>>
            Handle(GetPlexImage request, CancellationToken cancellationToken)
        {
            if (!_memoryCache.TryGetValue(request, out ConnectionParameters parameters))
            {
                Either<BaseError, ConnectionParameters> maybeParameters =
                    await Validate(request).Map(v => v.ToEither<ConnectionParameters>());
                return await maybeParameters.Match(
                    p =>
                    {
                        _memoryCache.Set(request, p, TimeSpan.FromHours(1));
                        return RequestImageFromPlex(request, p);
                    },
                    error => Left<BaseError, ImageViewModel>(error).AsTask());
            }

            return await RequestImageFromPlex(request, parameters);
        }

        private async Task<Either<BaseError, ImageViewModel>> RequestImageFromPlex(
            GetPlexImage request,
            ConnectionParameters connectionParameters)
        {
            var url = $"{connectionParameters.ActiveConnection.Uri}/{request.Path}";

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Plex-Token", connectionParameters.PlexServerAuthToken.AuthToken);
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return new ImageViewModel(
                    await response.Content.ReadAsByteArrayAsync(),
                    response.Content.Headers.ContentType?.MediaType ?? "image/jpeg");
            }

            return BaseError.New($"Unsuccessful response from plex image request: {response.StatusCode}");
        }

        private Task<Validation<BaseError, ConnectionParameters>> Validate(GetPlexImage request) =>
            PlexMediaSourceMustExist(request)
                .BindT(MediaSourceMustHaveActiveConnection)
                .BindT(MediaSourceMustHaveToken);

        private Task<Validation<BaseError, PlexMediaSource>> PlexMediaSourceMustExist(GetPlexImage request) =>
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
}
