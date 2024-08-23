using System.Diagnostics;
using System.Text;
using CliWrap;
using ErsatzTV.Application.Channels;
using ErsatzTV.Application.Images;
using ErsatzTV.Application.Streaming;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Iptv;
using ErsatzTV.Extensions;
using ErsatzTV.Filters;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[ServiceFilter(typeof(ConditionalIptvAuthorizeFilter))]
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

    [HttpHead("iptv/channels.m3u")]
    [HttpGet("iptv/channels.m3u")]
    public Task<IActionResult> GetChannelPlaylist(
        [FromQuery]
        string mode = "mixed") =>
        _mediator.Send(
                new GetChannelPlaylist(
                    Request.Scheme,
                    Request.Host.ToString(),
                    Request.PathBase,
                    mode,
                    Request.Headers.UserAgent,
                    Request.Query["access_token"]))
            .Map<ChannelPlaylist, IActionResult>(Ok);

    [HttpHead("iptv/xmltv.xml")]
    [HttpGet("iptv/xmltv.xml")]
    public Task<IActionResult> GetGuide() =>
        _mediator.Send(
                new GetChannelGuide(
                    Request.Scheme,
                    Request.Host.ToString(),
                    Request.PathBase,
                    Request.Query["access_token"]))
            .ToActionResult();

    [HttpHead("iptv/hdhr/channel/{channelNumber}.ts")]
    [HttpGet("iptv/hdhr/channel/{channelNumber}.ts")]
    public Task<IActionResult> GetHDHRVideo(string channelNumber, [FromQuery] string mode = "ts")
    {
        // don't redirect to the correct channel mode for HDHR clients; always use TS
        if (mode != "ts" && mode != "ts-legacy")
        {
            mode = "ts";
        }

        return GetTransportStreamVideo(channelNumber, mode);
    }

    [HttpHead("iptv/channel/{channelNumber}.ts")]
    [HttpGet("iptv/channel/{channelNumber}.ts")]
    public async Task<IActionResult> GetTransportStreamVideo(
        string channelNumber,
        [FromQuery]
        string mode = null)
    {
        // if mode is "unspecified" - find the configured mode and set it or redirect
        if (string.IsNullOrWhiteSpace(mode) || mode == "mixed")
        {
            Option<ChannelViewModel> maybeChannel = await _mediator.Send(new GetChannelByNumber(channelNumber));
            foreach (ChannelViewModel channel in maybeChannel)
            {
                switch (channel.StreamingMode)
                {
                    case StreamingMode.TransportStream:
                        mode = "ts-legacy";
                        break;
                    case StreamingMode.TransportStreamHybrid:
                        mode = "ts";
                        break;
                    default:
                        return Redirect($"~/iptv/channel/{channelNumber}.m3u8{AccessTokenQuery()}");
                }
            }
        }

        FFmpegProcessRequest request = mode switch
        {
            "ts-legacy" => new GetConcatProcessByChannelNumber(Request.Scheme, Request.Host.ToString(), channelNumber),
            _ => new GetWrappedProcessByChannelNumber(
                Request.Scheme,
                Request.Host.ToString(),
                Request.Query["access_token"],
                channelNumber)
        };

        return await _mediator.Send(request)
            .Map(
                result => result.Match<IActionResult>(
                    processModel =>
                    {
                        Command command = processModel.Process;

                        _logger.LogInformation("Starting ts stream for channel {ChannelNumber}", channelNumber);
                        _logger.LogDebug("ffmpeg arguments {FFmpegArguments}", command.Arguments);
                        var process = new FFmpegProcess
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = command.TargetFilePath,
                                Arguments = command.Arguments,
                                RedirectStandardOutput = true,
                                RedirectStandardError = false,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        HttpContext.Response.RegisterForDispose(process);

                        foreach ((string key, string value) in command.EnvironmentVariables)
                        {
                            process.StartInfo.Environment[key] = value;
                        }

                        process.Start();
                        return new FileStreamResult(process.StandardOutput.BaseStream, "video/mp2t");
                    },
                    error => BadRequest(error.Value)));
    }

    [HttpHead("iptv/session/{channelNumber}/hls.m3u8")]
    [HttpGet("iptv/session/{channelNumber}/hls.m3u8")]
    public async Task<IActionResult> GetLivePlaylist(string channelNumber, CancellationToken cancellationToken)
    {
        // _logger.LogDebug("Checking for session worker for channel {Channel}", channelNumber);

        if (_ffmpegSegmenterService.TryGetWorker(channelNumber, out IHlsSessionWorker worker) && worker is not null)
        {
            // _logger.LogDebug("Trimming playlist for channel {Channel}", channelNumber);

            DateTimeOffset now = DateTimeOffset.Now.AddSeconds(-30);
            Option<TrimPlaylistResult> maybePlaylist = await worker.TrimPlaylist(now, cancellationToken);
            foreach (TrimPlaylistResult result in maybePlaylist)
            {
                return Content(result.Playlist, "application/vnd.apple.mpegurl");
            }

            // TODO: better error here?
            _logger.LogWarning("Trim playlist failure; will return not found for channel {Channel}", channelNumber);
            return NotFound();
        }

        _logger.LogWarning("Unable to locate session worker for channel {Channel}; will redirect to start session", channelNumber);
        return RedirectToAction(nameof(GetHttpLiveStreamingVideo), new { channelNumber });
    }

    [HttpHead("iptv/channel/{channelNumber}.m3u8")]
    [HttpGet("iptv/channel/{channelNumber}.m3u8")]
    public async Task<IActionResult> GetHttpLiveStreamingVideo(
        string channelNumber,
        [FromQuery]
        string mode = "mixed")
    {
        // if mode is "unspecified" - find the configured mode and set it or redirect
        if (string.IsNullOrWhiteSpace(mode) || mode == "mixed")
        {
            Option<ChannelViewModel> maybeChannel = await _mediator.Send(new GetChannelByNumber(channelNumber));
            foreach (ChannelViewModel channel in maybeChannel)
            {
                switch (channel.StreamingMode)
                {
                    case StreamingMode.HttpLiveStreamingDirect:
                        mode = "hls-direct";
                        break;
                    case StreamingMode.HttpLiveStreamingSegmenter:
                        mode = "segmenter";
                        break;
                    case StreamingMode.HttpLiveStreamingSegmenterV2:
                        mode = "segmenter-v2";
                        break;
                    default:
                        return Redirect($"~/iptv/channel/{channelNumber}.ts{AccessTokenQuery()}");
                }
            }
        }

        switch (mode)
        {
            case "segmenter":
            case "segmenter-v2":
                _logger.LogDebug(
                    "Maybe starting ffmpeg session for channel {Channel}, mode {Mode}",
                    channelNumber,
                    mode);
                var request = new StartFFmpegSession(channelNumber, mode, Request.Scheme, Request.Host.ToString());
                Either<BaseError, Unit> result = await _mediator.Send(request);
                string multiVariantPlaylist = await GetMultiVariantPlaylist(channelNumber, mode);
                return result.Match<IActionResult>(
                    _ =>
                    {
                        _logger.LogDebug(
                            "Session started; returning multi-variant playlist for channel {Channel}",
                            channelNumber);

                        return Content(multiVariantPlaylist, "application/vnd.apple.mpegurl");
                        // return Redirect($"~/iptv/session/{channelNumber}/hls.m3u8");
                    },
                    error =>
                    {
                        switch (error)
                        {
                            case ChannelSessionAlreadyActive:
                                _logger.LogDebug(
                                    "Session is already active; returning multi-variant playlist for channel {Channel}",
                                    channelNumber);

                                return Content(multiVariantPlaylist, "application/vnd.apple.mpegurl");
                            // return RedirectPreserveMethod($"iptv/session/{channelNumber}/hls.m3u8");
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
                            playlist => Content(playlist, "application/vnd.apple.mpegurl"),
                            error => BadRequest(error.Value)));
        }
    }

    [HttpHead("iptv/logos/{fileName}")]
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

    private async Task<string> GetMultiVariantPlaylist(string channelNumber, string mode)
    {
        string file = mode switch
        {
            // this serves the unmodified playlist from disk
            "segmenter-v2" => "live.m3u8",

            _ => "hls.m3u8"
        };

        var variantPlaylist =
            $"{Request.Scheme}://{Request.Host}/iptv/session/{channelNumber}/{file}{AccessTokenQuery()}";

        try
        {
            if (mode == "segmenter-v2")
            {
                string fileName = Path.Combine(FileSystemLayout.TranscodeFolder, channelNumber, "playlist.m3u8");
                if (System.IO.File.Exists(fileName))
                {
                    string text = await System.IO.File.ReadAllTextAsync(fileName, Encoding.UTF8);
                    return text.Replace("live.m3u8", variantPlaylist);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to return ffmpeg multi-variant playlist; falling back to generated playlist");
        }

        Option<ResolutionViewModel> maybeResolution = await _mediator.Send(new GetChannelResolution(channelNumber));
        string resolution = string.Empty;
        foreach (ResolutionViewModel res in maybeResolution)
        {
            resolution = $",RESOLUTION={res.Width}x{res.Height}";
        }

        return $@"#EXTM3U
#EXT-X-VERSION:3
#EXT-X-STREAM-INF:BANDWIDTH=10000000{resolution}
{variantPlaylist}";
    }

    private string AccessTokenQuery() => string.IsNullOrWhiteSpace(Request.Query["access_token"])
        ? string.Empty
        : $"?access_token={Request.Query["access_token"]}";
}
