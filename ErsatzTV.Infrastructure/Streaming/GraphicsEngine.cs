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
                double totalSeconds = (double)frameCount / context.FrameRate;
                var timestamp = TimeSpan.FromSeconds(totalSeconds);

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
                                element.Draw(ctx, timestamp);
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
