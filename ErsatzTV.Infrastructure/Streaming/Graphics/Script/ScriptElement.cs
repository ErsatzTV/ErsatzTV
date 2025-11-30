using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;
using CliWrap;
using ErsatzTV.Core;
using ErsatzTV.Core.Graphics;
using ErsatzTV.Core.Interfaces.Streaming;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace ErsatzTV.Infrastructure.Streaming.Graphics;

public class ScriptElement(ScriptGraphicsElement scriptElement, ILogger logger)
    : GraphicsElement, IDisposable
{
    private CancellationTokenSource _cancellationTokenSource;
    private CommandTask<CommandResult> _commandTask;
    private int _frameSize;
    private PipeReader _pipeReader;
    private SKBitmap _canvasBitmap;
    private TimeSpan _startTime;
    private TimeSpan _endTime;

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _pipeReader?.Complete();

        _cancellationTokenSource?.Cancel();
        try
        {
#pragma warning disable VSTHRD002
            _commandTask?.Task.Wait();
#pragma warning restore VSTHRD002
        }
        catch (Exception)
        {
            // do nothing
        }

        _cancellationTokenSource?.Dispose();

        _canvasBitmap?.Dispose();
    }

    public override Task InitializeAsync(GraphicsEngineContext context, CancellationToken cancellationToken)
    {
        try
        {
            _startTime = TimeSpan.FromSeconds(scriptElement.StartSeconds ?? 0);
            _endTime = _startTime + TimeSpan.FromSeconds(scriptElement.DurationSeconds ?? 0);

            // already past the time when this is supposed to play; don't do any more work
            if (_endTime < context.Seek)
            {
                IsFinished = true;
                return Task.CompletedTask;
            }

            var options = new PipeOptions(
                minimumSegmentSize: 1024 * 1024,
                pauseWriterThreshold: 64 * 1024 * 1024,
                resumeWriterThreshold: 32 * 1024 * 1024
            );
            var pipe = new Pipe(options);
            _pipeReader = pipe.Reader;

            _frameSize = context.FrameSize.Width * context.FrameSize.Height * 4;

            // default to bgra, but allow rgba when configured
            SKColorType pixelFormat = SKColorType.Bgra8888;
            if (string.Equals(scriptElement.PixelFormat, "rgba", StringComparison.OrdinalIgnoreCase))
            {
                pixelFormat = SKColorType.Rgba8888;
            }

            _canvasBitmap = new SKBitmap(
                context.FrameSize.Width,
                context.FrameSize.Height,
                pixelFormat,
                SKAlphaType.Unpremul);

            string json = JsonSerializer.Serialize(context.TemplateVariables);

            Command command = Cli.Wrap(scriptElement.Command)
                .WithArguments(scriptElement.Arguments)
                .WithWorkingDirectory(FileSystemLayout.TempFilePoolFolder)
                .WithStandardInputPipe(PipeSource.FromString(json))
                .WithStandardOutputPipe(PipeTarget.ToStream(pipe.Writer.AsStream()));

            logger.LogDebug(
                "script element command {Command} arguments {Arguments}",
                command.TargetFilePath,
                command.Arguments);

            _cancellationTokenSource = new CancellationTokenSource();
            var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _cancellationTokenSource.Token);

            _commandTask = command.ExecuteAsync(linkedToken.Token);

            _ = _commandTask.Task.ContinueWith(_ => pipe.Writer.Complete(), TaskScheduler.Default);
        }
        catch (Exception ex)
        {
            IsFinished = true;
            logger.LogWarning(ex, "Failed to initialize motion element; will disable for this content");
        }

        return Task.CompletedTask;
    }

    public override async ValueTask<Option<PreparedElementImage>> PrepareImage(
        TimeSpan timeOfDay,
        TimeSpan contentTime,
        TimeSpan contentTotalTime,
        TimeSpan channelTime,
        CancellationToken cancellationToken)
    {
        try
        {
            if (contentTime < _startTime || contentTime > _endTime)
            {
                return Option<PreparedElementImage>.None;
            }

            while (true)
            {
                ReadResult readResult = await _pipeReader.ReadAsync(cancellationToken);
                ReadOnlySequence<byte> buffer = readResult.Buffer;
                SequencePosition consumed = buffer.Start;
                SequencePosition examined = buffer.End;

                try
                {
                    if (buffer.Length >= _frameSize)
                    {
                        ReadOnlySequence<byte> sequence = buffer.Slice(0, _frameSize);

                        using (SKPixmap pixmap = _canvasBitmap.PeekPixels())
                        {
                            sequence.CopyTo(pixmap.GetPixelSpan());
                        }

                        // mark this frame as consumed
                        consumed = sequence.End;

                        // we are done, return the frame
                        return new PreparedElementImage(_canvasBitmap, SKPointI.Empty, 1.0f, ZIndex, false);
                    }

                    if (readResult.IsCompleted)
                    {
                        await _pipeReader.CompleteAsync();

                        return Option<PreparedElementImage>.None;
                    }
                }
                finally
                {
                    // advance the reader, consuming the processed frame and examining the entire buffer
                    _pipeReader.AdvanceTo(consumed, examined);
                }
            }
        }
        catch (TaskCanceledException)
        {
            return Option<PreparedElementImage>.None;
        }
    }
}
