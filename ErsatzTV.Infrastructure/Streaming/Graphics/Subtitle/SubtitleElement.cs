using System.Buffers;
using System.IO.Pipelines;
using CliWrap;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Graphics;
using ErsatzTV.Core.Interfaces.FFmpeg;
using Microsoft.Extensions.Logging;
using Scriban;
using Scriban.Runtime;
using SkiaSharp;

namespace ErsatzTV.Infrastructure.Streaming.Graphics;

public class SubtitleElement(
    TemplateFunctions templateFunctions,
    ITempFilePool tempFilePool,
    SubtitleGraphicsElement subtitleElement,
    Dictionary<string, object> variables,
    ILogger logger)
    : GraphicsElement, IDisposable
{
    private CancellationTokenSource _cancellationTokenSource;
    private CommandTask<CommandResult> _commandTask;
    private int _frameSize;
    private PipeReader _pipeReader;
    private SKPointI _point;
    private SKBitmap _videoFrame;
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

        _videoFrame?.Dispose();
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

            // video size is the same as the main frame size
            _frameSize = frameSize.Width * frameSize.Height * 4;
            _videoFrame = new SKBitmap(frameSize.Width, frameSize.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);

            // subtitles contain their own positioning info
            _point = SKPointI.Empty;

            string subtitleTemplateFile = tempFilePool.GetNextTempFile(TempFileCategory.Subtitle);

            var scriptObject = new ScriptObject();
            scriptObject.Import(variables, renamer: member => member.Name);
            scriptObject.Import("convert_timezone", templateFunctions.ConvertTimeZone);
            scriptObject.Import("format_datetime", templateFunctions.FormatDateTime);

            var context = new TemplateContext { MemberRenamer = member => member.Name };
            context.PushGlobal(scriptObject);
            string inputText = await File.ReadAllTextAsync(subtitleElement.Template, cancellationToken);
            string textToRender = await Template.Parse(inputText).RenderAsync(context);
            await File.WriteAllTextAsync(subtitleTemplateFile, textToRender, cancellationToken);

            string subtitleFile = Path.GetFileName(subtitleTemplateFile);
            List<string> arguments =
            [
                "-nostdin", "-hide_banner", "-nostats", "-loglevel", "error",
                "-f", "lavfi",
                "-i",
                $"color=c=black@0.0:s={frameSize.Width}x{frameSize.Height}:r={frameRate},format=bgra,subtitles='{subtitleFile}':alpha=1",
                "-f", "image2pipe",
                "-pix_fmt", "bgra",
                "-vcodec", "rawvideo",
                "-"
            ];

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
            logger.LogWarning(ex, "Failed to initialize subtitle element; will disable for this content");
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

                    using (SKPixmap pixmap = _videoFrame.PeekPixels())
                    {
                        sequence.CopyTo(pixmap.GetPixelSpan());
                    }

                    // mark this frame as consumed
                    consumed = sequence.End;

                    // we are done, return the frame
                    return new PreparedElementImage(_videoFrame, _point, 1.0f, false);
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
}
