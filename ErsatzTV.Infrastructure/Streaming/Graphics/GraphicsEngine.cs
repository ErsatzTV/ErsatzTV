using System.IO.Pipelines;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Streaming;
using ErsatzTV.Core.Metadata;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace ErsatzTV.Infrastructure.Streaming.Graphics;

public class GraphicsEngine(
    TemplateFunctions templateFunctions,
    GraphicsEngineFonts graphicsEngineFonts,
    ITempFilePool tempFilePool,
    ITemplateDataRepository templateDataRepository,
    ILogger<GraphicsEngine> logger)
    : IGraphicsEngine
{
    public async Task Run(GraphicsEngineContext context, PipeWriter pipeWriter, CancellationToken cancellationToken)
    {
        graphicsEngineFonts.LoadFonts(FileSystemLayout.FontsCacheFolder);

        var templateVariables = new Dictionary<string, object>();

        // init template element variables once
        if (context.Elements.OfType<ITemplateDataContext>().Any())
        {
            // common variables
            templateVariables[MediaItemTemplateDataKey.Resolution] = context.FrameSize;
            templateVariables[MediaItemTemplateDataKey.StreamSeek] = context.Seek;

            // media item variables
            var maybeTemplateData =
                await templateDataRepository.GetMediaItemTemplateData(context.MediaItem);
            foreach (var templateData in maybeTemplateData)
            {
                foreach (var variable in templateData)
                {
                    templateVariables.Add(variable.Key, variable.Value);
                }
            }

            // epg variables
            int maxEpg = context.Elements.OfType<ITemplateDataContext>().Max(c => c.EpgEntries);
            var startTime = context.ContentStartTime + context.Seek;
            var maybeEpgData =
                await templateDataRepository.GetEpgTemplateData(context.ChannelNumber, startTime, maxEpg);
            foreach (var templateData in maybeEpgData)
            {
                foreach (var variable in templateData)
                {
                    templateVariables.Add(variable.Key, variable.Value);
                }
            }
        }

        var elements = new List<IGraphicsElement>();
        foreach (var element in context.Elements)
        {
            switch (element)
            {
                case WatermarkElementContext watermarkElementContext:
                    var watermark = new WatermarkElement(watermarkElementContext.Options, logger);
                    if (watermark.IsValid)
                    {
                        elements.Add(watermark);
                    }

                    break;

                case ImageElementContext imageElementContext:
                    elements.Add(new ImageElement(imageElementContext.ImageElement, logger));
                    break;

                case TextElementDataContext textElementContext:
                {
                    var variables = templateVariables.ToDictionary();
                    foreach (var variable in textElementContext.Variables)
                    {
                        variables.Add(variable.Key, variable.Value);
                    }

                    var textElement = new TextElement(
                        templateFunctions,
                        graphicsEngineFonts,
                        textElementContext.TextElement,
                        variables,
                        logger);

                    elements.Add(textElement);
                    break;
                }

                case SubtitleElementDataContext subtitleElementContext:
                {
                    var variables = templateVariables.ToDictionary();
                    foreach (var variable in subtitleElementContext.Variables)
                    {
                        variables.Add(variable.Key, variable.Value);
                    }

                    var subtitleElement = new SubtitleElement(
                        templateFunctions,
                        tempFilePool,
                        subtitleElementContext.SubtitlesElement,
                        variables,
                        logger);

                    elements.Add(subtitleElement);
                    break;
                }
            }
        }

        // initialize all elements
        await Task.WhenAll(
            elements.Select(e =>
                e.InitializeAsync(
                    context.SquarePixelFrameSize,
                    context.FrameSize,
                    context.FrameRate,
                    cancellationToken)));

        long frameCount = 0;
        var totalFrames = (long)(context.Duration.TotalSeconds * context.FrameRate);

        using var outputBitmap = new SKBitmap(
            context.FrameSize.Width,
            context.FrameSize.Height,
            SKColorType.Bgra8888,
            SKAlphaType.Premul);

        try
        {
            // `content_total_seconds` - the total number of seconds in the content
            var contentTotalTime = context.Seek + context.Duration;

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

                using var canvas = new SKCanvas(outputBitmap);
                canvas.Clear(SKColors.Transparent);

                // prepare images outside mutate to allow async image generation
                var preparedElementImages = new List<PreparedElementImage>();
                foreach (var element in elements.Where(e => !e.IsFailed).OrderBy(e => e.ZIndex))
                {
                    try
                    {
                        var maybePreparedImage = await element.PrepareImage(
                            frameTime.TimeOfDay,
                            contentTime,
                            contentTotalTime,
                            channelTime,
                            cancellationToken);

                        preparedElementImages.AddRange(maybePreparedImage);
                    }
                    catch (Exception ex)
                    {
                        element.IsFailed = true;
                        logger.LogWarning(
                            ex,
                            "Failed to draw graphics element of type {Type}; will disable for this content",
                            element.GetType().Name);
                    }
                }

                // draw each element
                using (var paint = new SKPaint())
                {
                    foreach (var preparedImage in preparedElementImages)
                    {
                        using (var colorFilter = SKColorFilter.CreateBlendMode(
                                   SKColors.White.WithAlpha((byte)(preparedImage.Opacity * 255)),
                                   SKBlendMode.Modulate))
                        {
                            paint.ColorFilter = colorFilter;
                            canvas.DrawBitmap(
                                preparedImage.Image,
                                new SKPoint(preparedImage.Point.X, preparedImage.Point.Y),
                                paint);
                        }

                        if (preparedImage.Dispose)
                        {
                            preparedImage.Image.Dispose();
                        }
                    }
                }

                // pipe output
                int frameBufferSize = context.FrameSize.Width * context.FrameSize.Height * 4;
                using (SKPixmap pixmap = outputBitmap.PeekPixels())
                {
                    var memory = pipeWriter.GetMemory(frameBufferSize);
                    pixmap.GetPixelSpan().CopyTo(memory.Span);
                }

                pipeWriter.Advance(frameBufferSize);
                await pipeWriter.FlushAsync(cancellationToken);

                frameCount++;
            }
        }
        catch (Exception)
        {
            // do nothing; don't want to throw on a background task
        }
        finally
        {
            await pipeWriter.CompleteAsync();

            foreach (var element in elements.OfType<IDisposable>())
            {
                element.Dispose();
            }
        }
    }
}
