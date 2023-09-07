using ErsatzTV.Application.Emby;
using ErsatzTV.Application.Images;
using ErsatzTV.Application.Jellyfin;
using ErsatzTV.Application.Plex;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Jellyfin;
using Flurl;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers;

[ResponseCache(Duration = 3600)]
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public class ArtworkController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMediator _mediator;

    public ArtworkController(IMediator mediator, IHttpClientFactory httpClientFactory)
    {
        _mediator = mediator;
        _httpClientFactory = httpClientFactory;
    }

    [HttpHead("/iptv/artwork/posters/{fileName}")]
    [HttpGet("/iptv/artwork/posters/{fileName}")]
    [HttpHead("/iptv/artwork/posters/{fileName}.jpg")]
    [HttpGet("/iptv/artwork/posters/{fileName}.jpg")]
    [HttpGet("/artwork/posters/{fileName}")]
    public async Task<IActionResult> GetPoster(string fileName, CancellationToken cancellationToken)
    {
        Either<BaseError, CachedImagePathViewModel> cachedImagePath =
            await _mediator.Send(new GetCachedImagePath(fileName, ArtworkKind.Poster, 440), cancellationToken);
        return cachedImagePath.Match<IActionResult>(
            Left: _ => new NotFoundResult(),
            Right: r => new PhysicalFileResult(r.FileName, r.MimeType));
    }

    [HttpGet("/artwork/watermarks/{fileName}")]
    public async Task<IActionResult> GetWatermark(string fileName, CancellationToken cancellationToken)
    {
        Either<BaseError, CachedImagePathViewModel> cachedImagePath =
            await _mediator.Send(new GetCachedImagePath(fileName, ArtworkKind.Watermark), cancellationToken);
        return cachedImagePath.Match<IActionResult>(
            Left: _ => new NotFoundResult(),
            Right: r => new PhysicalFileResult(r.FileName, r.MimeType));
    }

    [HttpGet("/artwork/fanart/{fileName}")]
    public async Task<IActionResult> GetFanArt(string fileName, CancellationToken cancellationToken)
    {
        Either<BaseError, CachedImagePathViewModel> cachedImagePath =
            await _mediator.Send(new GetCachedImagePath(fileName, ArtworkKind.FanArt), cancellationToken);
        return cachedImagePath.Match<IActionResult>(
            Left: _ => new NotFoundResult(),
            Right: r => new PhysicalFileResult(r.FileName, r.MimeType));
    }


    [HttpHead("/iptv/artwork/posters/jellyfin/{*path}")]
    [HttpGet("/iptv/artwork/posters/jellyfin/{*path}")]
    [HttpGet("/artwork/posters/jellyfin/{*path}")]
    [HttpHead("/iptv/artwork/thumbnails/jellyfin/{*path}")]
    [HttpGet("/iptv/artwork/thumbnails/jellyfin/{*path}")]
    [HttpGet("/artwork/thumbnails/jellyfin/{*path}")]
    [HttpGet("/artwork/fanart/jellyfin/{*path}")]
    public Task<IActionResult> GetJellyfin(string path, CancellationToken cancellationToken)
    {
        if (Request.QueryString.HasValue)
        {
            path += Request.QueryString.Value;
        }

        return GetJellyfinArtwork(path, cancellationToken);
    }

    [HttpHead("/iptv/artwork/posters/emby/{*path}")]
    [HttpGet("/iptv/artwork/posters/emby/{*path}")]
    [HttpGet("/artwork/posters/emby/{*path}")]
    [HttpHead("/iptv/artwork/thumbnails/emby/{*path}")]
    [HttpGet("/iptv/artwork/thumbnails/emby/{*path}")]
    [HttpGet("/artwork/thumbnails/emby/{*path}")]
    [HttpGet("/artwork/fanart/emby/{*path}")]
    public Task<IActionResult> GetEmby(string path, CancellationToken cancellationToken)
    {
        if (Request.QueryString.HasValue)
        {
            path += Request.QueryString.Value;
        }

        return GetEmbyArtwork(path, cancellationToken);
    }

    [HttpHead("/iptv/artwork/posters/plex/{plexMediaSourceId}/{*path}")]
    [HttpGet("/iptv/artwork/posters/plex/{plexMediaSourceId}/{*path}")]
    [HttpGet("/artwork/posters/plex/{plexMediaSourceId}/{*path}")]
    public Task<IActionResult> GetPlexPoster(int plexMediaSourceId, string path, CancellationToken cancellationToken) =>
        GetPlexArtwork(
            plexMediaSourceId,
            $"photo/:/transcode?url=/{path}&height=440&width=304&minSize=1&upscale=0",
            cancellationToken);

    [HttpGet("/artwork/fanart/plex/{plexMediaSourceId}/{*path}")]
    public Task<IActionResult> GetPlexFanArt(int plexMediaSourceId, string path, CancellationToken cancellationToken) =>
        GetPlexArtwork(plexMediaSourceId, $"/{path}", cancellationToken);

    [HttpGet("/artwork/thumbnails/plex/{plexMediaSourceId}/{*path}")]
    public Task<IActionResult> GetPlexThumbnail(
        int plexMediaSourceId,
        string path,
        CancellationToken cancellationToken) =>
        GetPlexArtwork(
            plexMediaSourceId,
            $"photo/:/transcode?url=/{path}&height=220&width=392&minSize=1&upscale=0",
            cancellationToken);

    [HttpHead("/iptv/artwork/thumbnails/{fileName}")]
    [HttpGet("/iptv/artwork/thumbnails/{fileName}")]
    [HttpHead("/iptv/artwork/thumbnails/{fileName}.jpg")]
    [HttpGet("/iptv/artwork/thumbnails/{fileName}.jpg")]
    [HttpGet("/artwork/thumbnails/{fileName}")]
    public async Task<IActionResult> GetThumbnail(string fileName, CancellationToken cancellationToken)
    {
        Either<BaseError, CachedImagePathViewModel> cachedImagePath =
            await _mediator.Send(new GetCachedImagePath(fileName, ArtworkKind.Thumbnail, 220), cancellationToken);
        return cachedImagePath.Match<IActionResult>(
            Left: _ => new NotFoundResult(),
            Right: r => new PhysicalFileResult(r.FileName, r.MimeType));
    }

    private async Task<IActionResult> GetPlexArtwork(
        int plexMediaSourceId,
        string transcodePath,
        CancellationToken cancellationToken)
    {
#if DEBUG_NO_SYNC
        await Task.CompletedTask;
        return NotFound();
#else
        Either<BaseError, PlexConnectionParametersViewModel> connectionParameters =
            await _mediator.Send(new GetPlexConnectionParameters(plexMediaSourceId), cancellationToken);

        return await connectionParameters.Match<Task<IActionResult>>(
            Left: _ => new NotFoundResult().AsTask<IActionResult>(),
            Right: async r =>
            {
                HttpClient client = _httpClientFactory.CreateClient();
                HttpContext.Response.RegisterForDispose(client);
                client.DefaultRequestHeaders.Add("X-Plex-Token", r.AuthToken);

                var fullPath = new Uri(r.Uri, transcodePath);
                HttpResponseMessage response = await client.GetAsync(
                    fullPath,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                HttpContext.Response.RegisterForDispose(response);

                Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                return new FileStreamResult(
                    stream,
                    response.Content.Headers.ContentType?.MediaType ?? "image/jpeg");
            });
#endif
    }

    private async Task<IActionResult> GetJellyfinArtwork(string path, CancellationToken cancellationToken)
    {
#if DEBUG_NO_SYNC
        await Task.CompletedTask;
        return NotFound();
#else
        Either<BaseError, JellyfinConnectionParametersViewModel> connectionParameters =
            await _mediator.Send(new GetJellyfinConnectionParameters(), cancellationToken);

        return await connectionParameters.Match<Task<IActionResult>>(
            Left: _ => new NotFoundResult().AsTask<IActionResult>(),
            Right: async vm =>
            {
                HttpClient client = _httpClientFactory.CreateClient();
                HttpContext.Response.RegisterForDispose(client);

                Url fullPath = JellyfinUrl.ForArtwork(vm.Address, path);
                HttpResponseMessage response = await client.GetAsync(
                    fullPath,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                HttpContext.Response.RegisterForDispose(response);

                Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                return new FileStreamResult(
                    stream,
                    response.Content.Headers.ContentType?.MediaType ?? "image/jpeg");
            });
#endif
    }

    private async Task<IActionResult> GetEmbyArtwork(string path, CancellationToken cancellationToken)
    {
#if DEBUG_NO_SYNC
        await Task.CompletedTask;
        return NotFound();
#else
        Either<BaseError, EmbyConnectionParametersViewModel> connectionParameters =
            await _mediator.Send(new GetEmbyConnectionParameters(), cancellationToken);

        return await connectionParameters.Match<Task<IActionResult>>(
            Left: _ => new NotFoundResult().AsTask<IActionResult>(),
            Right: async vm =>
            {
                HttpClient client = _httpClientFactory.CreateClient();
                HttpContext.Response.RegisterForDispose(client);

                Url fullPath = EmbyUrl.ForArtwork(vm.Address, path);
                HttpResponseMessage response = await client.GetAsync(
                    fullPath,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                HttpContext.Response.RegisterForDispose(response);

                Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                return new FileStreamResult(
                    stream,
                    response.Content.Headers.ContentType?.MediaType ?? "image/jpeg");
            });
#endif
    }
}
