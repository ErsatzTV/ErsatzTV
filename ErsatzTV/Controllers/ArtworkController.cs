using System.Threading.Tasks;
using ErsatzTV.Application.Images;
using ErsatzTV.Application.Images.Queries;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class PostersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PostersController(IMediator mediator) => _mediator = mediator;

        [HttpGet("/artwork/posters/{fileName}")]
        public async Task<IActionResult> GetPoster(string fileName)
        {
            Either<BaseError, ImageViewModel> imageContents =
                await _mediator.Send(new GetImageContents(fileName, ArtworkKind.Poster, 440));
            return imageContents.Match<IActionResult>(
                Left: _ => new NotFoundResult(),
                Right: r => new FileContentResult(r.Contents, r.MimeType));
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
