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
        public Task<IActionResult> GetChannelPlaylist(
            [FromQuery]
            string mode = "mixed") =>
            _mediator.Send(new GetChannelPlaylist(Request.Scheme, Request.Host.ToString(), mode))
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
                            // _logger.LogDebug(
                            //     "ffmpeg concat arguments {FFmpegArguments}",
                            //     string.Join(" ", process.StartInfo.ArgumentList));
                            process.Start();
                            return new FileStreamResult(process.StandardOutput.BaseStream, "video/mp2t");
                        },
                        error => BadRequest(error.Value)));

        [HttpGet("iptv/channel/{channelNumber}.m3u8")]
        public Task<IActionResult> GetHttpLiveStreamingVideo(
            string channelNumber,
            [FromQuery]
            string mode = "mixed") =>
            _mediator.Send(
                    new GetHlsPlaylistByChannelNumber(
                        Request.Scheme,
                        Request.Host.ToString(),
                        channelNumber,
                        mode))
                .Map(
                    result => result.Match<IActionResult>(
                        playlist => Content(playlist, "application/x-mpegurl"),
                        error => BadRequest(error.Value)));

        [HttpGet("iptv/logos/{fileName}")]
        [HttpHead("iptv/logos/{fileName}.jpg")]
        [HttpGet("iptv/logos/{fileName}.jpg")]
        public async Task<IActionResult> GetImage(string fileName)
        {
            Either<BaseError, CachedImagePathViewModel> cachedImagePath =
                await _mediator.Send(new GetCachedImagePath(fileName, ArtworkKind.Logo));
            return cachedImagePath.Match<IActionResult>(
                Left: _ => new NotFoundResult(),
                Right: r => new PhysicalFileResult(r.FileName, r.MimeType));
        }
    }
}
