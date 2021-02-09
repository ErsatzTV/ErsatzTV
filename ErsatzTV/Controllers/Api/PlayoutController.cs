using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Application.Playouts.Commands;
using ErsatzTV.Application.Playouts.Queries;
using ErsatzTV.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api
{
    [ApiController]
    [Route("api/playouts")]
    [Produces("application/json")]
    public class PlayoutController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PlayoutController(IMediator mediator) => _mediator = mediator;

        [HttpPost]
        [ProducesResponseType(typeof(PlayoutViewModel), 200)]
        [ProducesResponseType(400)]
        public Task<IActionResult> Add(
            [Required] [FromBody]
            CreatePlayout createPlayout) =>
            _mediator.Send(createPlayout).ToActionResult();

        [HttpGet("{playoutId}")]
        [ProducesResponseType(typeof(PlayoutViewModel), 200)]
        [ProducesResponseType(404)]
        public Task<IActionResult> Get(int playoutId) =>
            _mediator.Send(new GetPlayoutById(playoutId)).ToActionResult();

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<PlayoutViewModel>), 200)]
        public Task<IActionResult> GetAll() =>
            _mediator.Send(new GetAllPlayouts()).ToActionResult();

        [HttpPatch]
        [ProducesResponseType(typeof(PlayoutViewModel), 200)]
        [ProducesResponseType(400)]
        public Task<IActionResult> Update(
            [Required] [FromBody]
            UpdatePlayout updatePlayout) =>
            _mediator.Send(updatePlayout).ToActionResult();

        [HttpDelete]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public Task<IActionResult> Delete(
            [Required] [FromBody]
            DeletePlayout deletePlayout) =>
            _mediator.Send(deletePlayout).ToActionResult();
    }
}
