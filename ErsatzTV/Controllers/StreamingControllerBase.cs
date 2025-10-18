using System.IO.Pipelines;
using System.Text;
using CliWrap;
using ErsatzTV.Application.Streaming;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.Streaming;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers;

public abstract class StreamingControllerBase(IGraphicsEngine graphicsEngine, ILogger logger)
    : ControllerBase
{
    protected IActionResult GetProcessResponse(
        Either<BaseError, PlayoutItemProcessModel> result,
        string channelNumber,
        StreamingMode mode)
    {
        foreach (BaseError error in result.LeftToSeq())
        {
            logger.LogError(
                "Failed to create stream for channel {ChannelNumber}: {Error}",
                channelNumber,
                error.Value);

            return BadRequest(error.Value);
        }

        foreach (PlayoutItemProcessModel processModel in result.RightToSeq())
        {
            // for process counter
            var ffmpegProcess = new FFmpegProcess();

            Command process = processModel.Process;

            logger.LogDebug("ffmpeg arguments {FFmpegArguments}", process.Arguments);

            var cts = new CancellationTokenSource();
            HttpContext.Response.OnCompleted(async () =>
            {
                ffmpegProcess.Dispose();
                await cts.CancelAsync();
                cts.Dispose();
            });

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cts.Token,
                HttpContext.RequestAborted);

            var pipe = new Pipe();
            var stdErrBuffer = new StringBuilder();

            Command processWithPipe = process;
            foreach (GraphicsEngineContext graphicsEngineContext in processModel.GraphicsEngineContext)
            {
                var gePipe = new Pipe();
                processWithPipe = process.WithStandardInputPipe(PipeSource.FromStream(gePipe.Reader.AsStream()));

                // fire and forget graphics engine task
                _ = graphicsEngine.Run(
                    graphicsEngineContext,
                    gePipe.Writer,
                    linkedCts.Token);
            }

            CommandTask<CommandResult> task = processWithPipe
                .WithStandardOutputPipe(PipeTarget.ToStream(pipe.Writer.AsStream()))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync(linkedCts.Token);

            // ensure pipe writer is completed when ffmpeg exits
            _ = task.Task.ContinueWith(
                (_, state) => ((PipeWriter)state!).Complete(),
                pipe.Writer,
                TaskScheduler.Default);

            return new FileStreamResult(pipe.Reader.AsStream(), "video/mp2t");
        }

        // this will never happen
        return new NotFoundResult();
    }
}
