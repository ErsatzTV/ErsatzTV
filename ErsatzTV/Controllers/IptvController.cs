using System.Threading.Tasks;
using ErsatzTV.Application.Channels.Queries;
using ErsatzTV.Application.Images;
using ErsatzTV.Application.Images.Queries;
using ErsatzTV.Application.Streaming.Queries;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Iptv;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Controllers
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class IptvController : ControllerBase
    {
        private readonly ILogger<IptvController> _logger;
        private readonly IMediator _mediator;

        public IptvController(IMediator mediator, ILogger<IptvController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet("iptv/channels.m3u")]
        public Task<IActionResult> GetChannelPlaylist() =>
            _mediator.Send(new GetChannelPlaylist(Request.Scheme, Request.Host.ToString()))
                .Map<ChannelPlaylist, IActionResult>(Ok);

        [HttpGet("iptv/xmltv.xml")]
        public Task<IActionResult> GetGuide() =>
            _mediator.Send(new GetChannelGuide(Request.Scheme, Request.Host.ToString()))
                .Map<ChannelGuide, IActionResult>(Ok);

        [HttpGet("iptv/channel/{channelNumber}.ts")]
        public Task<IActionResult> GetTransportStreamVideo(string channelNumber) =>
            _mediator.Send(new GetConcatProcessByChannelNumber(Request.Scheme, Request.Host.ToString(), channelNumber))
                .Map(
                    result => result.Match<IActionResult>(
                        process =>
                        {
                            _logger.LogInformation("Starting ts stream for channel {ChannelNumber}", channelNumber);
                            process.Start();
                            return new FileStreamResult(process.StandardOutput.BaseStream, "video/mp2t");
                        },
                        error => BadRequest(error.Value)));

        [HttpGet("iptv/channel/{channelNumber}.m3u8")]
        public Task<IActionResult> GetHttpLiveStreamingVideo(string channelNumber) =>
            _mediator.Send(new GetHlsPlaylistByChannelNumber(Request.Scheme, Request.Host.ToString(), channelNumber))
                .Map(
                    result => result.Match<IActionResult>(
                        playlist =>
                        {
                            _logger.LogInformation("Starting hls stream for channel {ChannelNumber}", channelNumber);
                            return Content(playlist, "application/x-mpegurl");
                        },
                        error => BadRequest(error.Value)));

        [HttpGet("iptv/logos/{fileName}")]
        public async Task<IActionResult> GetImage(string fileName)
        {
            Either<BaseError, ImageViewModel> imageContents =
                await _mediator.Send(new GetImageContents(fileName, ArtworkKind.Logo));
            return imageContents.Match<IActionResult>(
                Left: _ => new NotFoundResult(),
                Right: r => new FileContentResult(r.Contents, r.MimeType));
        }
    }
}
