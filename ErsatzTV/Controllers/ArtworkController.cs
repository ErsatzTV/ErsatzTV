using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ErsatzTV.Application.Images;
using ErsatzTV.Application.Images.Queries;
using ErsatzTV.Application.Plex;
using ErsatzTV.Application.Plex.Queries;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
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

        [HttpGet("/artwork/posters/{fileName}")]
        public async Task<IActionResult> GetPoster(string fileName)
        {
            Either<BaseError, ImageViewModel> imageContents =
                await _mediator.Send(new GetImageContents(fileName, ArtworkKind.Poster, 440));
            return imageContents.Match<IActionResult>(
                Left: _ => new NotFoundResult(),
                Right: r => new FileContentResult(r.Contents, r.MimeType));
        }

        [HttpGet("/artwork/fanart/{fileName}")]
        public async Task<IActionResult> GetFanArt(string fileName)
        {
            Either<BaseError, ImageViewModel> imageContents =
                await _mediator.Send(new GetImageContents(fileName, ArtworkKind.FanArt));
            return imageContents.Match<IActionResult>(
                Left: _ => new NotFoundResult(),
                Right: r => new FileContentResult(r.Contents, r.MimeType));
        }

        [HttpGet("/artwork/posters/plex/{plexMediaSourceId}/{*path}")]
        public async Task<IActionResult> GetPlexPoster(int plexMediaSourceId, string path)
        {
            Either<BaseError, PlexConnectionParametersViewModel> connectionParameters =
                await _mediator.Send(new GetPlexConnectionParameters(plexMediaSourceId));

            return await connectionParameters.Match<Task<IActionResult>>(
                Left: _ => new NotFoundResult().AsTask<IActionResult>(),
                Right: async r =>
                {
                    HttpClient client = _httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.Add("X-Plex-Token", r.AuthToken);

                    var transcodePath = $"photo/:/transcode?url=/{path}&height=440&width=304&minSize=1&upscale=0";
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

        [HttpGet("/artwork/thumbnails/{fileName}")]
        public async Task<IActionResult> GetThumbnail(string fileName)
        {
            Either<BaseError, ImageViewModel> imageContents =
                await _mediator.Send(new GetImageContents(fileName, ArtworkKind.Thumbnail, 220));
            return imageContents.Match<IActionResult>(
                Left: _ => new NotFoundResult(),
                Right: r => new FileContentResult(r.Contents, r.MimeType));
        }
    }
}
