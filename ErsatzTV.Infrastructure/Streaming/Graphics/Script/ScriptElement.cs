using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Text.Json;
using CliWrap;
using ErsatzTV.Core;
using ErsatzTV.Core.Graphics;
using ErsatzTV.Core.Interfaces.Streaming;
using Humanizer;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace ErsatzTV.Infrastructure.Streaming.Graphics;

public class ScriptElement(ScriptGraphicsElement scriptElement, ILogger logger)
    : GraphicsElement, IDisposable
{
    private const uint EtvGraphicsMagic = 0x47565445;

    private CancellationTokenSource _cancellationTokenSource;
    private CommandTask<CommandResult> _commandTask;
    private int _frameSize;
    private PipeReader _pipeReader;
    private SKBitmap _canvasBitmap;
    private TimeSpan _startTime;
    private TimeSpan _endTime;
    private int _repeatCount;
    private long _totalBytes;

    public override int ZIndex { get; } = scriptElement.ZIndex ?? 0;

    public void Dispose()
    {
        logger.LogDebug(
            "Script element produced {ByteSize} ({Bytes} bytes)",
            ByteSize.FromBytes(_totalBytes),
            _totalBytes);

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
                "Script element command {Command} arguments {Arguments}",
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

            return scriptElement.Format is ScriptGraphicsFormat.Raw
                ? await PrepareFromRaw(cancellationToken)
                : await PrepareFromPacket(cancellationToken);
        }
        catch (TaskCanceledException)
        {
            return Option<PreparedElementImage>.None;
        }
    }

    private async Task<Option<PreparedElementImage>> PrepareFromRaw(CancellationToken cancellationToken)
    {
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

                    _totalBytes += _frameSize;

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

    private async Task<Option<PreparedElementImage>> PrepareFromPacket(CancellationToken cancellationToken)
    {
        if (_repeatCount > 0 || await ReadPacket(cancellationToken))
        {
            if (_repeatCount > 0)
            {
                _repeatCount--;
            }

            return new PreparedElementImage(_canvasBitmap, SKPointI.Empty, 1.0f, ZIndex, false);
        }

        IsFinished = true;
        await _pipeReader.CompleteAsync();
        return Option<PreparedElementImage>.None;
    }

    private async Task<bool> ReadPacket(CancellationToken cancellationToken)
    {
        // need 11 bytes - 4 magic, 2 version, 1 packet type, 4 payload len
        var result = await _pipeReader.ReadAtLeastAsync(11, cancellationToken);
        ReadOnlySequence<byte> buffer = result.Buffer;

        if (buffer.Length < 11)
        {
            return false;
        }

        Span<byte> headerBytes = stackalloc byte[11];
        buffer.Slice(0, 11).CopyTo(headerBytes);

        uint magic = BinaryPrimitives.ReadUInt32BigEndian(headerBytes[..4]);
        ushort version = BinaryPrimitives.ReadUInt16BigEndian(headerBytes.Slice(4, 2));
        byte type = headerBytes[6];
        uint payloadLen = BinaryPrimitives.ReadUInt32BigEndian(headerBytes.Slice(7, 4));

        if (magic != EtvGraphicsMagic || version != 1)
        {
            logger.LogWarning("Invalid graphics packet received: magic {Magic}, version {Version}", magic, version);
            return false;
        }

        // consume header
        _pipeReader.AdvanceTo(buffer.GetPosition(11));
        _totalBytes += 11;

        var success = true;

        if (payloadLen > 0)
        {
            result = await _pipeReader.ReadAtLeastAsync((int)payloadLen, cancellationToken);
            buffer = result.Buffer;

            switch (type)
            {
                case (byte)ScriptPayloadType.Full:
                    using (SKPixmap pixmap = _canvasBitmap.PeekPixels())
                    {
                        buffer.Slice(0, payloadLen).CopyTo(pixmap.GetPixelSpan());
                    }
                    break;
                case (byte)ScriptPayloadType.Repeat:
                    Span<byte> repeatBytes = stackalloc byte[4];
                    buffer.Slice(0, 4).CopyTo(repeatBytes);
                    _repeatCount = (int)BinaryPrimitives.ReadUInt32BigEndian(repeatBytes);
                    break;
                case (byte)ScriptPayloadType.Rectangles:
                    _canvasBitmap.Erase(SKColors.Transparent);
                    Span<byte> shortBytes = stackalloc byte[2];
                    buffer.Slice(0, shortBytes.Length).CopyTo(shortBytes);
                    int rectangleCount = BinaryPrimitives.ReadUInt16BigEndian(shortBytes);
                    int offset = shortBytes.Length;
                    using (SKPixmap pixmap = _canvasBitmap.PeekPixels())
                    {
                        int canvasWidth = pixmap.Width;
                        int canvasHeight = pixmap.Height;
                        const int BYTES_PER_PIXEL = 4;
                        int destRowBytes = pixmap.RowBytes;
                        Span<byte> canvasSpan = pixmap.GetPixelSpan();

                        for (int i = 0; i < rectangleCount; i++)
                        {
                            buffer.Slice(offset, 2).CopyTo(shortBytes);
                            offset += 2;
                            int x = BinaryPrimitives.ReadUInt16BigEndian(shortBytes);

                            buffer.Slice(offset, 2).CopyTo(shortBytes);
                            offset += 2;
                            int y = BinaryPrimitives.ReadUInt16BigEndian(shortBytes);

                            buffer.Slice(offset, 2).CopyTo(shortBytes);
                            offset += 2;
                            int w = BinaryPrimitives.ReadUInt16BigEndian(shortBytes);

                            buffer.Slice(offset, 2).CopyTo(shortBytes);
                            offset += 2;
                            int h = BinaryPrimitives.ReadUInt16BigEndian(shortBytes);

                            int effectiveW = Math.Max(0, Math.Min(w, canvasWidth - x));
                            int effectiveH = Math.Max(0, Math.Min(h, canvasHeight - y));

                            if (effectiveW > 0 && effectiveH > 0)
                            {
                                int sourceRowBytes = w * BYTES_PER_PIXEL;

                                for (int row = 0; row < effectiveH; row++)
                                {
                                    int sourceIndex = offset + (row * sourceRowBytes);
                                    int destIndex = ((y + row) * destRowBytes) + (x * BYTES_PER_PIXEL);

                                    buffer.Slice(sourceIndex, effectiveW * BYTES_PER_PIXEL)
                                        .CopyTo(canvasSpan.Slice(destIndex, effectiveW * BYTES_PER_PIXEL));
                                }
                            }

                            offset += w * h * 4;
                        }
                    }

                    break;
            }

            // consume payload
            _pipeReader.AdvanceTo(buffer.GetPosition(payloadLen));
            _totalBytes += payloadLen;
        }
        else
        {
            if (type == (byte)ScriptPayloadType.Clear)
            {
                _canvasBitmap.Erase(SKColors.Transparent);
            }
            else
            {
                logger.LogWarning("Unexpected zero-length payload for type {Type}", type);
                success = false;
            }
        }

        return success;
    }
}
