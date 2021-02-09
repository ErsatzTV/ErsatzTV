using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using ErsatzTV.Application.FFmpegProfiles;
using ErsatzTV.Application.FFmpegProfiles.Commands;
using ErsatzTV.Application.FFmpegProfiles.Queries;
using ErsatzTV.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api
{
    [ApiController]
    [Route("api/ffmpeg/profiles")]
    [Produces("application/json")]
    public class FFmpegProfileController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FFmpegProfileController(IMediator mediator) => _mediator = mediator;

        [HttpPost]
        [ProducesResponseType(typeof(FFmpegProfileViewModel), 200)]
        [ProducesResponseType(400)]
        public Task<IActionResult> Add(
            [Required] [FromBody]
            CreateFFmpegProfile createFFmpegProfile) =>
            _mediator.Send(createFFmpegProfile).ToActionResult();

        [HttpGet("{ffmpegProfileId}")]
        [ProducesResponseType(typeof(FFmpegProfileViewModel), 200)]
        [ProducesResponseType(404)]
        public Task<IActionResult> Get(int ffmpegProfileId) =>
            _mediator.Send(new GetFFmpegProfileById(ffmpegProfileId)).ToActionResult();

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<FFmpegProfileViewModel>), 200)]
        public Task<IActionResult> GetAll() =>
            _mediator.Send(new GetAllFFmpegProfiles()).ToActionResult();

        [HttpPatch]
        [ProducesResponseType(typeof(FFmpegProfileViewModel), 200)]
        [ProducesResponseType(400)]
        public Task<IActionResult> Update(
            [Required] [FromBody]
            UpdateFFmpegProfile updateFFmpegProfile) =>
            _mediator.Send(updateFFmpegProfile).ToActionResult();

        [HttpDelete]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public Task<IActionResult> Delete(
            [Required] [FromBody]
            DeleteFFmpegProfile deleteFFmpegProfile) =>
            _mediator.Send(deleteFFmpegProfile).ToActionResult();
    }
}
