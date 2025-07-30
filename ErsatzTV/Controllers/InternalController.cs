using System.Diagnostics;
using CliWrap;
using ErsatzTV.Application.Emby;
using ErsatzTV.Application.Jellyfin;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Application.Plex;
using ErsatzTV.Application.Streaming;
using ErsatzTV.Application.Subtitles.Queries;
using ErsatzTV.Core;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Extensions;
using Flurl;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public class InternalController : ControllerBase
{
    private readonly IFFmpegSegmenterService _ffmpegSegmenterService;
    private readonly ILogger<InternalController> _logger;
    private readonly IMediator _mediator;

    public InternalController(
        IFFmpegSegmenterService ffmpegSegmenterService,
        IMediator mediator,
        ILogger<InternalController> logger)
    {
        _ffmpegSegmenterService = ffmpegSegmenterService;
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("ffmpeg/concat/{channelNumber}")]
    public Task<IActionResult> GetConcatPlaylist(string channelNumber, [FromQuery] string mode = "ts-legacy") =>
        _mediator.Send(
                new GetConcatPlaylistByChannelNumber(Request.Scheme, Request.Host.ToString(), channelNumber, mode))
            .ToActionResult();

    [HttpGet("ffmpeg/stream/{channelNumber}")]
    public async Task<IActionResult> GetStream(
        string channelNumber,
        [FromQuery]
        string mode = "mixed")
    {
        switch (mode)
        {
            case "segmenter-v2":
                return await GetSegmenterV2Stream(channelNumber);
            default:
                return await GetTsLegacyStream(channelNumber, mode);
        }
    }

    [HttpGet("ffmpeg/remote-stream/{remoteStreamId}")]
    public async Task<IActionResult> GetRemoteStream(int remoteStreamId, CancellationToken cancellationToken)
    {
        Option<RemoteStreamViewModel> maybeRemoteStream =
            await _mediator.Send(new GetRemoteStreamById(remoteStreamId), cancellationToken);

        foreach (RemoteStreamViewModel remoteStream in maybeRemoteStream)
        {
            if (!string.IsNullOrWhiteSpace(remoteStream.Url))
            {
                return new RedirectResult(remoteStream.Url);
            }

            if (!string.IsNullOrWhiteSpace(remoteStream.Script))
            {
                string[] split =  remoteStream.Script.Split(" ");
                if (split.Length > 0)
                {
                    Command command = Cli.Wrap(split.Head());
                    if (split.Length > 1)
                    {
                        command = command.WithArguments(split.Tail());
                    }

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
                }
            }
        }

        return NotFound();

    }

    [HttpGet("/media/plex/{plexMediaSourceId:int}/{*path}")]
    public async Task<IActionResult> GetPlexMedia(
        int plexMediaSourceId,
        string path,
        CancellationToken cancellationToken)
    {
        Either<BaseError, PlexConnectionParametersViewModel> connectionParameters =
            await _mediator.Send(new GetPlexConnectionParameters(plexMediaSourceId), cancellationToken);

        return connectionParameters.Match<IActionResult>(
            Left: _ => new NotFoundResult(),
            Right: r =>
            {
                Url fullPath = new Uri(r.Uri, path).SetQueryParam("X-Plex-Token", r.AuthToken);
                return new RedirectResult(fullPath.ToString());
            });
    }

    [HttpGet("/media/jellyfin/{*path}")]
    public async Task<IActionResult> GetJellyfinMedia(string path, CancellationToken cancellationToken)
    {
        Either<BaseError, JellyfinConnectionParametersViewModel> connectionParameters =
            await _mediator.Send(new GetJellyfinConnectionParameters(), cancellationToken);

        return connectionParameters.Match<IActionResult>(
            Left: _ => new NotFoundResult(),
            Right: r =>
            {
                Url fullPath;

                if (path.Contains("Subtitles"))
                {
                    fullPath = Flurl.Url.Parse(r.Address)
                        .AppendPathSegment(path);
                }
                else
                {
                    fullPath = Flurl.Url.Parse(r.Address)
                        .AppendPathSegment("Videos")
                        .AppendPathSegment(path)
                        .AppendPathSegment("stream")
                        .SetQueryParam("static", "true");
                }

                return new RedirectResult(fullPath.ToString());
            });
    }

    [HttpGet("/media/emby/{*path}")]
    public async Task<IActionResult> GetEmbyMedia(string path, CancellationToken cancellationToken)
    {
        Either<BaseError, EmbyConnectionParametersViewModel> connectionParameters =
            await _mediator.Send(new GetEmbyConnectionParameters(), cancellationToken);

        return connectionParameters.Match<IActionResult>(
            Left: _ => new NotFoundResult(),
            Right: r =>
            {
                Url fullPath;

                if (path.Contains("Subtitles"))
                {
                    fullPath = Flurl.Url.Parse(r.Address)
                        .AppendPathSegment(path)
                        .SetQueryParam("X-Emby-Token", r.ApiKey);
                }
                else
                {
                    fullPath = Flurl.Url.Parse(r.Address)
                        .AppendPathSegment("Videos")
                        .AppendPathSegment(path)
                        .AppendPathSegment("stream")
                        .SetQueryParam("static", "true")
                        .SetQueryParam("X-Emby-Token", r.ApiKey);
                }

                return new RedirectResult(fullPath.ToString());
            });
    }

    [HttpGet("/media/subtitle/{id:int}")]
    public async Task<IActionResult> GetSubtitle(int id, [FromQuery] long? seekToMs)
    {
        Either<BaseError, string> maybePath = await _mediator.Send(new GetSubtitlePathById(id));

        foreach (string path in maybePath.RightToSeq())
        {
            string mimeType = Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".ass" or ".ssa" => "text/x-ssa",
                ".vtt" => "text/vtt",
                _ => "application/x-subrip"
            };

            if (seekToMs is > 0)
            {
                Either<BaseError, SeekTextSubtitleProcess> maybeProcess = await _mediator.Send(
                    new GetSeekTextSubtitleProcess(path, TimeSpan.FromMilliseconds(seekToMs.Value)));
                foreach (SeekTextSubtitleProcess processModel in maybeProcess.RightToSeq())
                {
                    Command command = processModel.Process;

                    _logger.LogDebug("ffmpeg text subtitle arguments {FFmpegArguments}", command.Arguments);

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
                    return new FileStreamResult(process.StandardOutput.BaseStream, mimeType);
                }

                return new NotFoundResult();
            }

            if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return new RedirectResult(path);
            }

            return new PhysicalFileResult(path, mimeType);
        }

        return new NotFoundResult();
    }

    private async Task<IActionResult> GetSegmenterV2Stream(string channelNumber)
    {
        if (_ffmpegSegmenterService.TryGetWorker(channelNumber, out IHlsSessionWorker worker) &&
            worker is HlsSessionWorkerV2 v2)
        {
            Either<BaseError, PlayoutItemProcessModel> result = await v2.GetNextPlayoutItemProcess();
            return GetProcessResponse(result, channelNumber, "segmenter-v2");
        }

        _logger.LogWarning("Unable to locate session worker for channel {Channel}", channelNumber);
        return new NotFoundResult();
    }

    private async Task<IActionResult> GetTsLegacyStream(string channelNumber, string mode)
    {
        var request = new GetPlayoutItemProcessByChannelNumber(
            channelNumber,
            mode,
            DateTimeOffset.Now,
            false,
            true,
            0,
            Option<int>.None);

        Either<BaseError, PlayoutItemProcessModel> result = await _mediator.Send(request);

        return GetProcessResponse(result, channelNumber, mode);
    }

    private IActionResult GetProcessResponse(
        Either<BaseError, PlayoutItemProcessModel> result,
        string channelNumber,
        string mode)
    {
        foreach (BaseError error in result.LeftToSeq())
        {
            _logger.LogError(
                "Failed to create stream for channel {ChannelNumber}: {Error}",
                channelNumber,
                error.Value);

            return BadRequest(error.Value);
        }

        foreach (PlayoutItemProcessModel processModel in result.RightToSeq())
        {
            Command command = processModel.Process;

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

            var contentType = "video/mp2t";
            if (mode.Equals("hls-direct", StringComparison.OrdinalIgnoreCase))
            {
                contentType = "video/mp4";
            }

            process.Start();
            return new FileStreamResult(process.StandardOutput.BaseStream, contentType);
        }

        // this will never happen
        return new NotFoundResult();
    }
}
