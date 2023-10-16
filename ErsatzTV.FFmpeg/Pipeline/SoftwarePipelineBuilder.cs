using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.FFmpeg.Decoder;
using ErsatzTV.FFmpeg.Encoder;
using ErsatzTV.FFmpeg.Filter;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.InputOption;
using ErsatzTV.FFmpeg.OutputOption;
using ErsatzTV.FFmpeg.State;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Pipeline;

public class SoftwarePipelineBuilder : PipelineBuilderBase
{
    private readonly ILogger _logger;

    public SoftwarePipelineBuilder(
        IFFmpegCapabilities ffmpegCapabilities,
        HardwareAccelerationMode hardwareAccelerationMode,
        Option<VideoInputFile> videoInputFile,
        Option<AudioInputFile> audioInputFile,
        Option<WatermarkInputFile> watermarkInputFile,
        Option<SubtitleInputFile> subtitleInputFile,
        string reportsFolder,
        string fontsFolder,
        ILogger logger) : base(
        ffmpegCapabilities,
        hardwareAccelerationMode,
        videoInputFile,
        audioInputFile,
        watermarkInputFile,
        subtitleInputFile,
        reportsFolder,
        fontsFolder,
        logger) =>
        _logger = logger;

    protected override bool IsIntelVaapiOrQsv(FFmpegState ffmpegState) => false;

    protected override bool IsNvidiaOnWindows(FFmpegState ffmpegState) => false;

    protected override FFmpegState SetAccelState(
        VideoStream videoStream,
        FFmpegState ffmpegState,
        FrameState desiredState,
        PipelineContext context,
        ICollection<IPipelineStep> pipelineSteps) => ffmpegState with
    {
        DecoderHardwareAccelerationMode = HardwareAccelerationMode.None,
        EncoderHardwareAccelerationMode = HardwareAccelerationMode.None
    };

    protected override Option<IDecoder> SetDecoder(
        VideoInputFile videoInputFile,
        VideoStream videoStream,
        FFmpegState ffmpegState,
        PipelineContext context)
    {
        foreach (IDecoder decoder in GetSoftwareDecoder(videoStream))
        {
            videoInputFile.AddOption(decoder);
            return Some(decoder);
        }

        return None;
    }

    protected virtual Option<IEncoder> GetEncoder(
        FFmpegState ffmpegState,
        FrameState currentState,
        FrameState desiredState) =>
        GetSoftwareEncoder(currentState, desiredState);

    protected override FilterChain SetVideoFilters(
        VideoInputFile videoInputFile,
        VideoStream videoStream,
        Option<WatermarkInputFile> watermarkInputFile,
        Option<SubtitleInputFile> subtitleInputFile,
        PipelineContext context,
        Option<IDecoder> maybeDecoder,
        FFmpegState ffmpegState,
        FrameState desiredState,
        string fontsFolder,
        ICollection<IPipelineStep> pipelineSteps)
    {
        var watermarkOverlayFilterSteps = new List<IPipelineFilterStep>();
        var subtitleOverlayFilterSteps = new List<IPipelineFilterStep>();

        FrameState currentState = desiredState with
        {
            PixelFormat = videoStream.PixelFormat,
            FrameDataLocation = FrameDataLocation.Software,
            IsAnamorphic = videoStream.IsAnamorphic,
            ScaledSize = videoStream.FrameSize,
            PaddedSize = videoStream.FrameSize
        };

        foreach (IDecoder decoder in maybeDecoder)
        {
            currentState = decoder.NextState(currentState);
        }

        if (desiredState.VideoFormat != VideoFormat.Copy)
        {
            SetDeinterlace(videoInputFile, context, currentState);

            currentState = SetScale(videoInputFile, videoStream, desiredState, currentState);
            currentState = SetPad(videoInputFile, videoStream, desiredState, currentState);
            currentState = SetCrop(videoInputFile, desiredState, currentState);
            SetSubtitle(
                videoInputFile,
                subtitleInputFile,
                context,
                desiredState,
                fontsFolder,
                subtitleOverlayFilterSteps);
            SetWatermark(
                videoStream,
                watermarkInputFile,
                context,
                ffmpegState,
                desiredState,
                currentState,
                watermarkOverlayFilterSteps);
        }

        // after everything else is done, apply the encoder
        if (pipelineSteps.OfType<IEncoder>().All(e => e.Kind != StreamKind.Video))
        {
            foreach (IEncoder encoder in GetEncoder(ffmpegState, currentState, desiredState))
            {
                pipelineSteps.Add(encoder);
                videoInputFile.FilterSteps.Add(encoder);
            }
        }

        // after decoder/encoder, return hls direct
        if (desiredState.VideoFormat == VideoFormat.Copy)
        {
            return FilterChain.Empty;
        }

        List<IPipelineFilterStep> pixelFormatFilterSteps = SetPixelFormat(
            videoStream,
            desiredState.PixelFormat,
            currentState,
            pipelineSteps);

        return new FilterChain(
            videoInputFile.FilterSteps,
            watermarkInputFile.Map(wm => wm.FilterSteps).IfNone(new List<IPipelineFilterStep>()),
            subtitleInputFile.Map(st => st.FilterSteps).IfNone(new List<IPipelineFilterStep>()),
            watermarkOverlayFilterSteps,
            subtitleOverlayFilterSteps,
            pixelFormatFilterSteps);
    }

    protected virtual List<IPipelineFilterStep> SetPixelFormat(
        VideoStream videoStream,
        Option<IPixelFormat> desiredPixelFormat,
        FrameState currentState,
        ICollection<IPipelineStep> pipelineSteps)
    {
        var result = new List<IPipelineFilterStep>();

        foreach (IPixelFormat pixelFormat in desiredPixelFormat)
        {
            if (!videoStream.ColorParams.IsBt709)
            {
                // _logger.LogDebug("Adding colorspace filter");
                var colorspace = new ColorspaceFilter(currentState, videoStream, pixelFormat);
                currentState = colorspace.NextState(currentState);
                result.Add(colorspace);
            }

            if (currentState.PixelFormat.Map(f => f.FFmpegName) != pixelFormat.FFmpegName)
            {
                _logger.LogDebug(
                    "Format {A} doesn't equal {B}",
                    currentState.PixelFormat.Map(f => f.FFmpegName),
                    pixelFormat.FFmpegName);

                pipelineSteps.Add(new PixelFormatOutputOption(pixelFormat));
            }
        }

        return result;
    }

    private void SetWatermark(
        VideoStream videoStream,
        Option<WatermarkInputFile> watermarkInputFile,
        PipelineContext context,
        FFmpegState ffmpegState,
        FrameState desiredState,
        FrameState currentState,
        List<IPipelineFilterStep> watermarkOverlayFilterSteps)
    {
        if (context.HasWatermark)
        {
            WatermarkInputFile watermark = watermarkInputFile.Head();

            watermark.FilterSteps.Add(
                new WatermarkPixelFormatFilter(ffmpegState, watermark.DesiredState, context.Is10BitOutput));

            foreach (VideoStream watermarkStream in watermark.VideoStreams)
            {
                if (watermarkStream.StillImage == false)
                {
                    watermark.AddOption(new DoNotIgnoreLoopInputOption());
                }
                else if (watermark.DesiredState.MaybeFadePoints.Map(fp => fp.Count > 0).IfNone(false))
                {
                    // looping is required to fade a static image in and out
                    watermark.AddOption(new InfiniteLoopInputOption(HardwareAccelerationMode.None));
                }
            }

            if (watermark.DesiredState.Size == WatermarkSize.Scaled)
            {
                watermark.FilterSteps.Add(
                    new WatermarkScaleFilter(watermark.DesiredState, currentState.PaddedSize));
            }

            if (watermark.DesiredState.Opacity != 100)
            {
                watermark.FilterSteps.Add(new WatermarkOpacityFilter(watermark.DesiredState));
            }

            foreach (List<WatermarkFadePoint> fadePoints in watermark.DesiredState.MaybeFadePoints)
            {
                watermark.FilterSteps.AddRange(fadePoints.Map(fp => new WatermarkFadeFilter(fp)));
            }

            foreach (IPixelFormat desiredPixelFormat in desiredState.PixelFormat)
            {
                IPixelFormat pf = desiredPixelFormat;
                if (desiredPixelFormat is PixelFormatNv12 nv12)
                {
                    foreach (IPixelFormat availablePixelFormat in AvailablePixelFormats.ForPixelFormat(nv12.Name, null))
                    {
                        pf = availablePixelFormat;
                    }
                }

                var watermarkFilter = new OverlayWatermarkFilter(
                    watermark.DesiredState,
                    desiredState.PaddedSize,
                    videoStream.SquarePixelFrameSize(currentState.PaddedSize),
                    pf,
                    _logger);
                watermarkOverlayFilterSteps.Add(watermarkFilter);
            }
        }
    }

    private static void SetSubtitle(
        VideoInputFile videoInputFile,
        Option<SubtitleInputFile> subtitleInputFile,
        PipelineContext context,
        FrameState desiredState,
        string fontsFolder,
        ICollection<IPipelineFilterStep> subtitleOverlayFilterSteps)
    {
        foreach (SubtitleInputFile subtitle in subtitleInputFile)
        {
            if (context.HasSubtitleText)
            {
                videoInputFile.AddOption(new CopyTimestampInputOption());

                var subtitlesFilter = new SubtitlesFilter(fontsFolder, subtitle);
                videoInputFile.FilterSteps.Add(subtitlesFilter);
            }
            else if (context.HasSubtitleOverlay)
            {
                // only scale if scaling or padding was used for main video stream
                if (videoInputFile.FilterSteps.Any(s => s is ScaleFilter or PadFilter))
                {
                    var scaleFilter = new ScaleImageFilter(desiredState.PaddedSize);
                    subtitle.FilterSteps.Add(scaleFilter);
                }

                foreach (IPixelFormat desiredPixelFormat in desiredState.PixelFormat)
                {
                    IPixelFormat pf = desiredPixelFormat;
                    if (desiredPixelFormat is PixelFormatNv12 nv12)
                    {
                        foreach (IPixelFormat availablePixelFormat in AvailablePixelFormats.ForPixelFormat(
                                     nv12.Name,
                                     null))
                        {
                            pf = availablePixelFormat;
                        }
                    }

                    var subtitlesFilter = new OverlaySubtitleFilter(pf);
                    subtitleOverlayFilterSteps.Add(subtitlesFilter);
                }
            }
        }
    }

    private static FrameState SetPad(
        VideoInputFile videoInputFile,
        VideoStream videoStream,
        FrameState desiredState,
        FrameState currentState)
    {
        if (desiredState.CroppedSize.IsNone && currentState.PaddedSize != desiredState.PaddedSize)
        {
            IPipelineFilterStep padStep = new PadFilter(currentState, desiredState.PaddedSize);
            currentState = padStep.NextState(currentState);
            videoInputFile.FilterSteps.Add(padStep);
        }

        return currentState;
    }

    private static FrameState SetScale(
        VideoInputFile videoInputFile,
        VideoStream videoStream,
        FrameState desiredState,
        FrameState currentState)
    {
        if (videoStream.FrameSize != desiredState.ScaledSize)
        {
            IPipelineFilterStep scaleStep = new ScaleFilter(
                currentState,
                desiredState.ScaledSize,
                desiredState.PaddedSize,
                desiredState.CroppedSize,
                VideoStream.IsAnamorphicEdgeCase);

            currentState = scaleStep.NextState(currentState);

            videoInputFile.FilterSteps.Add(scaleStep);
        }

        return currentState;
    }

    private static void SetDeinterlace(VideoInputFile videoInputFile, PipelineContext context, FrameState currentState)
    {
        if (context.ShouldDeinterlace)
        {
            videoInputFile.FilterSteps.Add(new YadifFilter(currentState));
        }
    }
}
