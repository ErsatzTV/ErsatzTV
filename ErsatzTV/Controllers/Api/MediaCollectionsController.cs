using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaCollections.Commands;
using ErsatzTV.Application.MediaCollections.Queries;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api
{
    [ApiController]
    [Route("api/media/collections")]
    [Produces("application/json")]
    public class MediaCollectionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MediaCollectionsController(IMediator mediator) => _mediator = mediator;

        [HttpPost]
        [ProducesResponseType(typeof(MediaCollectionViewModel), 200)]
        [ProducesResponseType(400)]
        public Task<IActionResult> Add(
            [Required] [FromBody]
            CreateSimpleMediaCollection createCollection) =>
            _mediator.Send(createCollection).ToActionResult();

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<MediaCollectionViewModel>), 200)]
        public Task<IActionResult> GetAll() =>
            _mediator.Send(new GetAllMediaCollections()).ToActionResult();

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(MediaCollectionViewModel), 200)]
        [ProducesResponseType(404)]
        public Task<IActionResult> Get(int id) =>
            _mediator.Send(new GetSimpleMediaCollectionById(id)).ToActionResult();

        [HttpGet("{id}/items")]
        [ProducesResponseType(typeof(IEnumerable<MediaItemViewModel>), 200)]
        [ProducesResponseType(404)]
        public Task<IActionResult> GetItems(int id) =>
            _mediator.Send(new GetSimpleMediaCollectionItems(id)).ToActionResult();

        [HttpPut("{id}/items")]
        [ProducesResponseType(typeof(IEnumerable<MediaItemViewModel>), 200)]
        [ProducesResponseType(404)]
        public Task<IActionResult> PutItems(
            int id,
            [Required] [FromBody]
            List<int> mediaItemIds) =>
            _mediator.Send(new ReplaceSimpleMediaCollectionItems(id, mediaItemIds)).ToActionResult();
    }
}
