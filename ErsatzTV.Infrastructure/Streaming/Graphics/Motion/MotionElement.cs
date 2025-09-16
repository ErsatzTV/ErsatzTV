using System.Buffers;
using System.IO.Pipelines;
using CliWrap;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Graphics;
using ErsatzTV.Core.Interfaces.Metadata;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace ErsatzTV.Infrastructure.Streaming.Graphics;

public class MotionElement(
    MotionGraphicsElement motionElement,
    Option<string> ffprobePath,
    ILocalStatisticsProvider localStatisticsProvider,
    ILogger logger)
    : GraphicsElement, IDisposable
{
    private CancellationTokenSource _cancellationTokenSource;
    private CommandTask<CommandResult> _commandTask;
    private int _frameSize;
    private PipeReader _pipeReader;
    private SKPointI _point;
    private SKBitmap _canvasBitmap;
    private SKBitmap _motionFrameBitmap;
    private bool _isFinished;

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
        _motionFrameBitmap?.Dispose();
    }

    public override async Task InitializeAsync(
        Resolution squarePixelFrameSize,
        Resolution frameSize,
        int frameRate,
        CancellationToken cancellationToken)
    {
        try
        {
            var pipe = new Pipe();
            _pipeReader = pipe.Reader;

            var sizeAndDecoder = await ProbeMotionElement(frameSize);

            _frameSize = sizeAndDecoder.Size.Width * sizeAndDecoder.Size.Height * 4;

            _canvasBitmap = new SKBitmap(frameSize.Width, frameSize.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);

            _motionFrameBitmap = new SKBitmap(
                sizeAndDecoder.Size.Width,
                sizeAndDecoder.Size.Height,
                SKColorType.Bgra8888,
                SKAlphaType.Unpremul);

            _point = SKPointI.Empty;

            int scaledWidth = sizeAndDecoder.Size.Width;
            int scaledHeight = sizeAndDecoder.Size.Height;

            (int horizontalMargin, int verticalMargin) = NormalMargins(
                frameSize,
                motionElement.HorizontalMarginPercent ?? 0,
                motionElement.VerticalMarginPercent ?? 0);

            _point = CalculatePosition(
                motionElement.Location,
                frameSize.Width,
                frameSize.Height,
                scaledWidth,
                scaledHeight,
                horizontalMargin,
                verticalMargin);

            List<string> arguments = ["-nostdin", "-hide_banner", "-nostats", "-loglevel", "error"];

            foreach (string decoder in sizeAndDecoder.Decoder)
            {
                arguments.AddRange(["-c:v", decoder]);
            }

            arguments.AddRange(
            [
                "-i", motionElement.VideoPath,
                "-f", "image2pipe",
                "-pix_fmt", "bgra",
                "-vcodec", "rawvideo",
                "-"
            ]);

            Command command = Cli.Wrap("ffmpeg")
                .WithArguments(arguments)
                .WithWorkingDirectory(FileSystemLayout.TempFilePoolFolder)
                .WithStandardOutputPipe(PipeTarget.ToStream(pipe.Writer.AsStream()));

            _cancellationTokenSource = new CancellationTokenSource();
            var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _cancellationTokenSource.Token);

            _commandTask = command.ExecuteAsync(linkedToken.Token);

            _ = _commandTask.Task.ContinueWith(_ => pipe.Writer.Complete(), TaskScheduler.Default);
        }
        catch (Exception ex)
        {
            IsFailed = true;
            logger.LogWarning(ex, "Failed to initialize motion element; will disable for this content");
        }
    }

    public override async ValueTask<Option<PreparedElementImage>> PrepareImage(
        TimeSpan timeOfDay,
        TimeSpan contentTime,
        TimeSpan contentTotalTime,
        TimeSpan channelTime,
        CancellationToken cancellationToken)
    {
        if (_isFinished)
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

                    using (SKPixmap pixmap = _motionFrameBitmap.PeekPixels())
                    {
                        sequence.CopyTo(pixmap.GetPixelSpan());
                    }

                    _canvasBitmap.Erase(SKColors.Transparent);

                    using (var canvas = new SKCanvas(_canvasBitmap))
                    {
                        canvas.DrawBitmap(_motionFrameBitmap, _point);
                    }

                    // mark this frame as consumed
                    consumed = sequence.End;

                    // we are done, return the frame
                    return new PreparedElementImage(_canvasBitmap, SKPointI.Empty, 1.0f, false);
                }

                if (readResult.IsCompleted)
                {
                    _isFinished = true;

                    await _pipeReader.CompleteAsync();
                    return Option<PreparedElementImage>.None;
                }
            }
            finally
            {
                if (!_isFinished)
                {
                    // advance the reader, consuming the processed frame and examining the entire buffer
                    _pipeReader.AdvanceTo(consumed, examined);
                }
            }
        }
    }

    private async Task<SizeAndDecoder> ProbeMotionElement(Resolution frameSize)
    {
        try
        {
            foreach (string ffprobe in ffprobePath)
            {
                Either<BaseError, MediaVersion> maybeMediaVersion =
                    await localStatisticsProvider.GetStatistics(ffprobe, motionElement.VideoPath);

                foreach (var mediaVersion in maybeMediaVersion.RightToSeq())
                {
                    Option<string> decoder = Option<string>.None;

                    foreach (var videoStream in mediaVersion.Streams.Where(s =>
                                 s.MediaStreamKind is MediaStreamKind.Video))
                    {
                        decoder = videoStream.Codec switch
                        {
                            "vp8" => "libvpx",
                            "vp9" => "libvpx-vp9",
                            _ => Option<string>.None
                        };
                    }

                    return new SizeAndDecoder(
                        new Resolution { Width = mediaVersion.Width, Height = mediaVersion.Height },
                        decoder);
                }
            }
        }
        catch (Exception)
        {
            // do nothing
        }

        return new SizeAndDecoder(frameSize, Option<string>.None);
    }

    private record SizeAndDecoder(Resolution Size, Option<string> Decoder);
}
