using System.IO.Pipelines;
using ErsatzTV.Core.Interfaces.Streaming;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ErsatzTV.Infrastructure.Streaming;

public class GraphicsEngine(ILogger<GraphicsEngine> logger) : IGraphicsEngine
{
    public async Task Run(GraphicsEngineContext context, PipeWriter pipeWriter, CancellationToken cancellationToken)
    {
        var elements = new List<IGraphicsElement>();
        foreach (var element in context.Elements)
        {
            switch (element)
            {
                case WatermarkElementContext watermarkElementContext:
                    var watermark = new WatermarkElement(watermarkElementContext.Options);
                    if (watermark.IsValid)
                    {
                        elements.Add(watermark);
                    }

                    break;
            }
        }

        // initialize all elements
        await Task.WhenAll(elements.Select(e => e.InitializeAsync(context.FrameSize, context.FrameRate, cancellationToken)));

        long frameCount = 0;
        var totalFrames = (long)(context.Duration.TotalSeconds * context.FrameRate);

        try
        {
            while (!cancellationToken.IsCancellationRequested && frameCount < totalFrames)
            {
                // seconds since this specific stream started
                double streamTimeSeconds = (double)frameCount / context.FrameRate;
                var streamTime = TimeSpan.FromSeconds(streamTimeSeconds);

                // `content_seconds` - the total number of seconds the frame is into the content
                var contentTime = context.Seek + streamTime;

                // `time_of_day_seconds` - the total number of seconds the frame is since midnight
                var frameTime = context.ContentStartTime + contentTime;

                // `channel_seconds` - the total number of seconds the frame is from when the channel started/activated
                var channelTime = frameTime - context.ChannelStartTime;

                using var outputFrame = new Image<Bgra32>(
                    context.FrameSize.Width,
                    context.FrameSize.Height,
                    Color.Transparent);

                // draw each element
                outputFrame.Mutate(ctx =>
                {
                    foreach (var element in elements)
                    {
                        try
                        {
                            if (!element.IsFailed)
                            {
                                element.Draw(ctx, frameTime.TimeOfDay, contentTime, channelTime, cancellationToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            element.IsFailed = true;
                            logger.LogWarning(ex,
                                "Failed to draw graphics element of type {Type}; will disable for this content",
                                element.GetType().Name);
                        }
                    }
                });

                // pipe output
                int frameBufferSize = context.FrameSize.Width * context.FrameSize.Height * 4;
                Memory<byte> memory = pipeWriter.GetMemory(frameBufferSize);
                outputFrame.CopyPixelDataTo(memory.Span);
                pipeWriter.Advance(frameBufferSize);
                await pipeWriter.FlushAsync(cancellationToken);

                frameCount++;
            }
        }
        finally
        {
            foreach (var element in elements.OfType<IDisposable>())
            {
                element.Dispose();
            }
        }
    }
}
