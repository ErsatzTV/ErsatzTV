using System.Diagnostics;
using CliWrap;
using ErsatzTV.Application.Streaming;
using ErsatzTV.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

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
                            var process = new Process
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
}