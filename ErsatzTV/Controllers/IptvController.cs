using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ErsatzTV.Application.Channels.Queries;
using ErsatzTV.Application.Images;
using ErsatzTV.Application.Images.Queries;
using ErsatzTV.Application.Streaming.Commands;
using ErsatzTV.Application.Streaming.Queries;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Iptv;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Controllers
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class IptvController : ControllerBase
    {
        private readonly IFFmpegSegmenterService _ffmpegSegmenterService;
        private readonly ILogger<IptvController> _logger;
        private readonly IMediator _mediator;

        public IptvController(
            IMediator mediator,
            ILogger<IptvController> logger,
            IFFmpegSegmenterService ffmpegSegmenterService)
        {
            _mediator = mediator;
            _logger = logger;
            _ffmpegSegmenterService = ffmpegSegmenterService;
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
        public async Task<IActionResult> GetTransportStreamVideo(
            string channelNumber,
            [FromQuery]
            string mode = null)
        {
            FFmpegProcessRequest request = mode switch
            {
                "ts-legacy" => new GetConcatProcessByChannelNumber(Request.Scheme, Request.Host.ToString(), channelNumber),
                _ => new GetWrappedProcessByChannelNumber(Request.Scheme, Request.Host.ToString(), channelNumber)
            };

            return await _mediator.Send(request)
                .Map(
                    result => result.Match<IActionResult>(
                        processModel =>
                        {
                            Process process = processModel.Process;

                            _logger.LogInformation("Starting ts stream for channel {ChannelNumber}", channelNumber);
                            // _logger.LogDebug(
                            //     "ffmpeg concat arguments {FFmpegArguments}",
                            //     string.Join(" ", process.StartInfo.ArgumentList));
                            process.Start();
                            return new FileStreamResult(process.StandardOutput.BaseStream, "video/mp2t");
                        },
                        error => BadRequest(error.Value)));
        }

        [HttpGet("iptv/session/{channelNumber}/hls.m3u8")]
        public async Task<IActionResult> GetLivePlaylist(string channelNumber)
        {
            if (_ffmpegSegmenterService.SessionWorkers.TryGetValue(channelNumber, out IHlsSessionWorker worker))
            {
                DateTimeOffset now = DateTimeOffset.Now.AddSeconds(-30);
            
                string fileName = Path.Combine(FileSystemLayout.TranscodeFolder, channelNumber, "live.m3u8");
                if (System.IO.File.Exists(fileName))
                {
                    string[] input = await System.IO.File.ReadAllLinesAsync(fileName);

                    TrimPlaylistResult result = HlsPlaylistFilter.TrimPlaylist(worker.PlaylistStart, now, input);
                    return Content(result.Playlist, "application/vnd.apple.mpegurl");
                }
            }

            return NotFound();
        }

        [HttpGet("iptv/channel/{channelNumber}.m3u8")]
        public async Task<IActionResult> GetHttpLiveStreamingVideo(
            string channelNumber,
            [FromQuery]
            string mode = "mixed")
        {
            switch (mode)
            {
                case "segmenter":
                    Either<BaseError, Unit> result = await _mediator.Send(new StartFFmpegSession(channelNumber, false));
                    return result.Match<IActionResult>(
                        _ => Redirect($"/iptv/session/{channelNumber}/hls.m3u8"),
                        error =>
                        {
                            switch (error)
                            {
                                case ChannelSessionAlreadyActive:
                                    return RedirectPreserveMethod($"/iptv/session/{channelNumber}/hls.m3u8");
                                default:
                                    _logger.LogWarning(
                                        "Failed to start segmenter for channel {ChannelNumber}: {Error}",
                                        channelNumber,
                                        error.ToString());
                                    return NotFound();
                            }
                        });
                default:
                    return await _mediator.Send(
                            new GetHlsPlaylistByChannelNumber(
                                Request.Scheme,
                                Request.Host.ToString(),
                                channelNumber,
                                mode))
                        .Map(
                            r => r.Match<IActionResult>(
                                playlist => Content(playlist, "application/x-mpegurl"),
                                error => BadRequest(error.Value)));
            }
        }

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
