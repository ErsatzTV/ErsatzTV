using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using ErsatzTV.Application.Channels;
using ErsatzTV.Application.Channels.Commands;
using ErsatzTV.Application.Channels.Queries;
using ErsatzTV.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api
{
    [ApiController]
    [Route("api/channels")]
    [Produces("application/json")]
    public class ChannelsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ChannelsController(IMediator mediator) => _mediator = mediator;

        [HttpPost]
        [ProducesResponseType(typeof(ChannelViewModel), 200)]
        [ProducesResponseType(400)]
        public Task<IActionResult> Add(
            [Required] [FromBody]
            CreateChannel createChannel) =>
            _mediator.Send(createChannel).ToActionResult();

        [HttpGet("{channelId}")]
        [ProducesResponseType(typeof(ChannelViewModel), 200)]
        [ProducesResponseType(404)]
        public Task<IActionResult> Get(int channelId) =>
            _mediator.Send(new GetChannelById(channelId)).ToActionResult();

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ChannelViewModel>), 200)]
        public Task<IActionResult> GetAll() =>
            _mediator.Send(new GetAllChannels()).ToActionResult();

        [HttpPatch]
        [ProducesResponseType(typeof(ChannelViewModel), 200)]
        [ProducesResponseType(400)]
        public Task<IActionResult> Update(
            [Required] [FromBody]
            UpdateChannel updateChannel) =>
            _mediator.Send(updateChannel).ToActionResult();

        [HttpDelete]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public Task<IActionResult> Delete(
            [Required] [FromBody]
            DeleteChannel deleteChannel) =>
            _mediator.Send(deleteChannel).ToActionResult();
    }
}
