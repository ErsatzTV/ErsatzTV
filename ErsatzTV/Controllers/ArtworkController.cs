using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ErsatzTV.Application.Emby;
using ErsatzTV.Application.Emby.Queries;
using ErsatzTV.Application.Images;
using ErsatzTV.Application.Images.Queries;
using ErsatzTV.Application.Jellyfin;
using ErsatzTV.Application.Jellyfin.Queries;
using ErsatzTV.Application.Plex;
using ErsatzTV.Application.Plex.Queries;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Jellyfin;
using Flurl;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers
{
    [ResponseCache(Duration = 3600)]
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class PostersController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMediator _mediator;

        public PostersController(IMediator mediator, IHttpClientFactory httpClientFactory)
        {
            _mediator = mediator;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("/iptv/artwork/posters/{fileName}")]
        [HttpGet("/iptv/artwork/posters/{fileName}.jpg")]
        [HttpGet("/artwork/posters/{fileName}")]
        public async Task<IActionResult> GetPoster(string fileName)
        {
            Either<BaseError, CachedImagePathViewModel> cachedImagePath =
                await _mediator.Send(new GetCachedImagePath(fileName, ArtworkKind.Poster, 440));
            return cachedImagePath.Match<IActionResult>(
                Left: _ => new NotFoundResult(),
                Right: r => new PhysicalFileResult(r.FileName, r.MimeType));
        }

        [HttpGet("/artwork/watermarks/{fileName}")]
        public async Task<IActionResult> GetWatermark(string fileName)
        {
            Either<BaseError, CachedImagePathViewModel> cachedImagePath =
                await _mediator.Send(new GetCachedImagePath(fileName, ArtworkKind.Watermark));
            return cachedImagePath.Match<IActionResult>(
                Left: _ => new NotFoundResult(),
                Right: r => new PhysicalFileResult(r.FileName, r.MimeType));
        }

        [HttpGet("/artwork/fanart/{fileName}")]
        public async Task<IActionResult> GetFanArt(string fileName)
        {
            Either<BaseError, CachedImagePathViewModel> cachedImagePath =
                await _mediator.Send(new GetCachedImagePath(fileName, ArtworkKind.FanArt));
            return cachedImagePath.Match<IActionResult>(
                Left: _ => new NotFoundResult(),
                Right: r => new PhysicalFileResult(r.FileName, r.MimeType));
        }


        [HttpGet("/iptv/artwork/posters/jellyfin/{*path}")]
        [HttpGet("/artwork/posters/jellyfin/{*path}")]
        [HttpGet("/iptv/artwork/thumbnails/jellyfin/{*path}")]
        [HttpGet("/artwork/thumbnails/jellyfin/{*path}")]
        [HttpGet("/artwork/fanart/jellyfin/{*path}")]
        public Task<IActionResult> GetJellyfin(string path)
        {
            if (Request.QueryString.HasValue)
            {
                path += Request.QueryString.Value;
            }

            return GetJellyfinArtwork(path);
        }

        [HttpGet("/iptv/artwork/posters/emby/{*path}")]
        [HttpGet("/artwork/posters/emby/{*path}")]
        [HttpGet("/iptv/artwork/thumbnails/emby/{*path}")]
        [HttpGet("/artwork/thumbnails/emby/{*path}")]
        [HttpGet("/artwork/fanart/emby/{*path}")]
        public Task<IActionResult> GetEmby(string path)
        {
            if (Request.QueryString.HasValue)
            {
                path += Request.QueryString.Value;
            }

            return GetEmbyArtwork(path);
        }

        [HttpGet("/iptv/artwork/posters/plex/{plexMediaSourceId}/{*path}")]
        [HttpGet("/artwork/posters/plex/{plexMediaSourceId}/{*path}")]
        public Task<IActionResult> GetPlexPoster(int plexMediaSourceId, string path) =>
            GetPlexArtwork(
                plexMediaSourceId,
                $"photo/:/transcode?url=/{path}&height=440&width=304&minSize=1&upscale=0");

        [HttpGet("/artwork/fanart/plex/{plexMediaSourceId}/{*path}")]
        public Task<IActionResult> GetPlexFanArt(int plexMediaSourceId, string path) =>
            GetPlexArtwork(
                plexMediaSourceId,
                $"/{path}");

        [HttpGet("/artwork/thumbnails/plex/{plexMediaSourceId}/{*path}")]
        public Task<IActionResult> GetPlexThumbnail(int plexMediaSourceId, string path) =>
            GetPlexArtwork(
                plexMediaSourceId,
                $"photo/:/transcode?url=/{path}&height=220&width=392&minSize=1&upscale=0");

        [HttpGet("/iptv/artwork/thumbnails/{fileName}")]
        [HttpGet("/iptv/artwork/thumbnails/{fileName}.jpg")]
        [HttpGet("/artwork/thumbnails/{fileName}")]
        public async Task<IActionResult> GetThumbnail(string fileName)
        {
            Either<BaseError, CachedImagePathViewModel> cachedImagePath =
                await _mediator.Send(new GetCachedImagePath(fileName, ArtworkKind.Thumbnail, 220));
            return cachedImagePath.Match<IActionResult>(
                Left: _ => new NotFoundResult(),
                Right: r => new PhysicalFileResult(r.FileName, r.MimeType));
        }

        private async Task<IActionResult> GetPlexArtwork(int plexMediaSourceId, string transcodePath)
        {
            Either<BaseError, PlexConnectionParametersViewModel> connectionParameters =
                await _mediator.Send(new GetPlexConnectionParameters(plexMediaSourceId));

            return await connectionParameters.Match<Task<IActionResult>>(
                Left: _ => new NotFoundResult().AsTask<IActionResult>(),
                Right: async r =>
                {
                    HttpClient client = _httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.Add("X-Plex-Token", r.AuthToken);

                    var fullPath = new Uri(r.Uri, transcodePath);
                    HttpResponseMessage response = await client.GetAsync(
                        fullPath,
                        HttpCompletionOption.ResponseHeadersRead);
                    Stream stream = await response.Content.ReadAsStreamAsync();

                    return new FileStreamResult(
                        stream,
                        response.Content.Headers.ContentType?.MediaType ?? "image/jpeg");
                });
        }

        private async Task<IActionResult> GetJellyfinArtwork(string path)
        {
            Either<BaseError, JellyfinConnectionParametersViewModel> connectionParameters =
                await _mediator.Send(new GetJellyfinConnectionParameters());

            return await connectionParameters.Match<Task<IActionResult>>(
                Left: _ => new NotFoundResult().AsTask<IActionResult>(),
                Right: async vm =>
                {
                    HttpClient client = _httpClientFactory.CreateClient();

                    Url fullPath = JellyfinUrl.ForArtwork(vm.Address, path);
                    HttpResponseMessage response = await client.GetAsync(
                        fullPath,
                        HttpCompletionOption.ResponseHeadersRead);
                    Stream stream = await response.Content.ReadAsStreamAsync();

                    return new FileStreamResult(
                        stream,
                        response.Content.Headers.ContentType?.MediaType ?? "image/jpeg");
                });
        }

        private async Task<IActionResult> GetEmbyArtwork(string path)
        {
            Either<BaseError, EmbyConnectionParametersViewModel> connectionParameters =
                await _mediator.Send(new GetEmbyConnectionParameters());

            return await connectionParameters.Match<Task<IActionResult>>(
                Left: _ => new NotFoundResult().AsTask<IActionResult>(),
                Right: async vm =>
                {
                    HttpClient client = _httpClientFactory.CreateClient();

                    Url fullPath = EmbyUrl.ForArtwork(vm.Address, path);
                    HttpResponseMessage response = await client.GetAsync(
                        fullPath,
                        HttpCompletionOption.ResponseHeadersRead);
                    Stream stream = await response.Content.ReadAsStreamAsync();

                    return new FileStreamResult(
                        stream,
                        response.Content.Headers.ContentType?.MediaType ?? "image/jpeg");
                });
        }
    }
}
