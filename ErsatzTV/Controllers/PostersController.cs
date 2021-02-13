using System.Threading.Tasks;
using ErsatzTV.Application.Images;
using ErsatzTV.Application.MediaItems.Queries;
using ErsatzTV.Core;
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

        [HttpGet("/posters/{mediaItemId}")]
        public async Task<IActionResult> ForMediaItem(int mediaItemId)
        {
            Either<BaseError, ImageViewModel> imageContents = await _mediator.Send(new GetPosterContents(mediaItemId));
            return imageContents.Match<IActionResult>(
                Left: _ => new NotFoundResult(),
                Right: r => new FileContentResult(r.Contents, r.MimeType));
        }
    }
}
