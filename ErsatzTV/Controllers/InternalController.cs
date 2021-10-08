using System.Threading.Tasks;
using ErsatzTV.Application.Streaming.Queries;
using ErsatzTV.Extensions;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Controllers
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class InternalController : ControllerBase
    {
        private readonly ILogger<InternalController> _logger;
        private readonly IMediator _mediator;

        public InternalController(IMediator mediator, ILogger<InternalController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet("ffmpeg/concat/{channelNumber}")]
        public Task<IActionResult> GetConcatPlaylist(string channelNumber) =>
            _mediator.Send(new GetConcatPlaylistByChannelNumber(Request.Scheme, Request.Host.ToString(), channelNumber))
                .ToActionResult();

        [HttpGet("ffmpeg/stream/{channelNumber}")]
        public Task<IActionResult> GetStream(
            string channelNumber,
            [FromQuery]
            string mode = "mixed") =>
            _mediator.Send(new GetPlayoutItemProcessByChannelNumber(channelNumber, mode, false)).Map(
                result =>
                    result.Match<IActionResult>(
                        process =>
                        {
                            _logger.LogDebug(
                                "ffmpeg arguments {FFmpegArguments}",
                                string.Join(" ", process.StartInfo.ArgumentList));
                            process.Start();
                            return new FileStreamResult(process.StandardOutput.BaseStream, "video/mp2t");
                        },
                        error =>
                        {
                            _logger.LogError(
                                "Failed to create stream for channel {ChannelNumber}: {Error}",
                                channelNumber,
                                error.Value);
                            return BadRequest(error.Value);
                        }
                    ));
    }
}
