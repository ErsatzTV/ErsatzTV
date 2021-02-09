using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Application.MediaSources;
using ErsatzTV.Application.MediaSources.Queries;
using ErsatzTV.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api
{
    [ApiController]
    [Route("api/media/sources")]
    [Produces("application/json")]
    public class MediaSourcesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MediaSourcesController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<MediaSourceViewModel>), 200)]
        public Task<IActionResult> GetAll() =>
            _mediator.Send(new GetAllMediaSources()).ToActionResult();

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(MediaSourceViewModel), 200)]
        [ProducesResponseType(404)]
        public Task<IActionResult> Get(int id) =>
            _mediator.Send(new GetMediaSourceById(id)).ToActionResult();
    }
}
