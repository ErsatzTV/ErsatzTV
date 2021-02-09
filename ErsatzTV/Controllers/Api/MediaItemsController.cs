using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Application.MediaItems.Commands;
using ErsatzTV.Application.MediaItems.Queries;
using ErsatzTV.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api
{
    [ApiController]
    [Route("api/media/items")]
    [Produces("application/json")]
    public class MediaItemsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MediaItemsController(IMediator mediator) => _mediator = mediator;

        [HttpPost]
        [ProducesResponseType(typeof(MediaItemViewModel), 200)]
        [ProducesResponseType(400)]
        public Task<IActionResult> Add(
            [Required] [FromBody]
            CreateMediaItem createMediaItem) =>
            _mediator.Send(createMediaItem).ToActionResult();

        [HttpGet("{mediaItemId}")]
        [ProducesResponseType(typeof(MediaItemViewModel), 200)]
        [ProducesResponseType(404)]
        public Task<IActionResult> Get(int mediaItemId) =>
            _mediator.Send(new GetMediaItemById(mediaItemId)).ToActionResult();

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<MediaItemViewModel>), 200)]
        public Task<IActionResult> GetAll() =>
            _mediator.Send(new GetAllMediaItems()).ToActionResult();

        [HttpDelete]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public Task<IActionResult> Delete(
            [Required] [FromBody]
            DeleteMediaItem deleteMediaItem) =>
            _mediator.Send(deleteMediaItem).ToActionResult();
    }
}
