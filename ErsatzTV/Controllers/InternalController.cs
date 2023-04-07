using System.Diagnostics;
using CliWrap;
using ErsatzTV.Application.Emby;
using ErsatzTV.Application.Jellyfin;
using ErsatzTV.Application.Plex;
using ErsatzTV.Application.Streaming;
using ErsatzTV.Application.Subtitles.Queries;
using ErsatzTV.Core;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Flurl;

namespace ErsatzTV.Controllers;

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
        _mediator.Send(
                new GetPlayoutItemProcessByChannelNumber(
                    channelNumber,
                    mode,
                    DateTimeOffset.Now,
                    false,
                    true,
                    0,
                    Option<int>.None))
            .Map(
                result =>
                    result.Match<IActionResult>(
                        processModel =>
                        {
                            Command command = processModel.Process;

                            _logger.LogInformation("ffmpeg arguments {FFmpegArguments}", command.Arguments);
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
                        error =>
                        {
                            _logger.LogError(
                                "Failed to create stream for channel {ChannelNumber}: {Error}",
                                channelNumber,
                                error.Value);
                            return BadRequest(error.Value);
                        }
                    ));

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
                Url fullPath =  new Uri(r.Uri, path).SetQueryParam("X-Plex-Token", r.AuthToken);
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
    public async Task<IActionResult> GetSubtitle(int id)
    {
        Either<BaseError, string> path = await _mediator.Send(new GetSubtitlePathById(id));
        return path.Match<IActionResult>(
            Left: _ => new NotFoundResult(),
            Right: r =>
            {
                if (r.StartsWith("http"))
                {
                    return new RedirectResult(r);
                }
                
                string mimeType = Path.GetExtension(r).ToLowerInvariant() switch
                {
                    "ass" or "ssa" => "text/x-ssa",
                    "vtt" => "text/vtt",
                    _ => "application/x-subrip",
                };

                return new PhysicalFileResult(r, mimeType);
            });
    }
}
