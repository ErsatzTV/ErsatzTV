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
    SubtitlesGraphicsElement subtitlesElement,
    Dictionary<string, object> variables,
    ILogger logger)
    : GraphicsElement, IDisposable
{
    private CommandTask<CommandResult> _commandTask;
    private CancellationTokenSource _cancellationTokenSource;
    private PipeReader _pipeReader;
    private SKBitmap _videoFrame;
    private int _frameSize;
    private SKPointI _point;

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

            var subtitleTemplateFile = tempFilePool.GetNextTempFile(TempFileCategory.Subtitle);

            var scriptObject = new ScriptObject();
            scriptObject.Import(variables, renamer: member => member.Name);
            scriptObject.Import("convert_timezone", templateFunctions.ConvertTimeZone);
            scriptObject.Import("format_datetime", templateFunctions.FormatDateTime);

            var context = new TemplateContext { MemberRenamer = member => member.Name };
            context.PushGlobal(scriptObject);
            var inputText = await File.ReadAllTextAsync(subtitlesElement.Template, cancellationToken);
            string textToRender = await Template.Parse(inputText).RenderAsync(context);
            await File.WriteAllTextAsync(subtitleTemplateFile, textToRender, cancellationToken);

            var subtitleFile = Path.GetFileName(subtitleTemplateFile);
            List<string> arguments =
            [
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
        while (true)
        {
            ReadResult readResult = await _pipeReader.ReadAsync(cancellationToken);
            var buffer = readResult.Buffer;
            var consumed = buffer.Start;
            var examined = buffer.End;

            try
            {
                if (buffer.Length >= _frameSize)
                {
                    var sequence = buffer.Slice(0, _frameSize);

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
}
