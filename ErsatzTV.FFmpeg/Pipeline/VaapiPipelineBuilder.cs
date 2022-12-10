using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.FFmpeg.Decoder;
using ErsatzTV.FFmpeg.Encoder;
using ErsatzTV.FFmpeg.Encoder.Vaapi;
using ErsatzTV.FFmpeg.Filter;
using ErsatzTV.FFmpeg.Filter.Vaapi;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.Option;
using ErsatzTV.FFmpeg.Option.HardwareAcceleration;
using ErsatzTV.FFmpeg.State;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Pipeline;

public class VaapiPipelineBuilder : SoftwarePipelineBuilder
{
    private readonly IHardwareCapabilities _hardwareCapabilities;
    private readonly ILogger _logger;

    public VaapiPipelineBuilder(
        IHardwareCapabilities hardwareCapabilities,
        HardwareAccelerationMode hardwareAccelerationMode,
        Option<VideoInputFile> videoInputFile,
        Option<AudioInputFile> audioInputFile,
        Option<WatermarkInputFile> watermarkInputFile,
        Option<SubtitleInputFile> subtitleInputFile,
        string reportsFolder,
        string fontsFolder,
        ILogger logger) : base(
        hardwareAccelerationMode,
        videoInputFile,
        audioInputFile,
        watermarkInputFile,
        subtitleInputFile,
        reportsFolder,
        fontsFolder,
        logger)
    {
        _hardwareCapabilities = hardwareCapabilities;
        _logger = logger;
    }

    protected override FFmpegState SetAccelState(
        VideoStream videoStream,
        FFmpegState ffmpegState,
        FrameState desiredState,
        PipelineContext context,
        ICollection<IPipelineStep> pipelineSteps)
    {
        bool canDecode = _hardwareCapabilities.CanDecode(
            videoStream.Codec,
            desiredState.VideoProfile,
            videoStream.PixelFormat);
        bool canEncode = _hardwareCapabilities.CanEncode(
            desiredState.VideoFormat,
            desiredState.VideoProfile,
            desiredState.PixelFormat);

        foreach (string vaapiDevice in ffmpegState.VaapiDevice)
        {
            pipelineSteps.Add(new VaapiHardwareAccelerationOption(vaapiDevice));
        }

        // use software decoding with an extensive pipeline
        if (context.HasSubtitleOverlay && context.HasWatermark)
        {
            canDecode = false;
        }

        // disable hw accel if decoder/encoder isn't supported
        return ffmpegState with
        {
            DecoderHardwareAccelerationMode = canDecode
                ? HardwareAccelerationMode.Vaapi
                : HardwareAccelerationMode.None,
            EncoderHardwareAccelerationMode = canEncode
                ? HardwareAccelerationMode.Vaapi
                : HardwareAccelerationMode.None
        };
    }

    protected override void SetDecoder(
        VideoInputFile videoInputFile,
        VideoStream videoStream,
        FFmpegState ffmpegState,
        PipelineContext context,
        ICollection<IPipelineStep> pipelineSteps)
    {
        Option<IDecoder> maybeDecoder = (ffmpegState.DecoderHardwareAccelerationMode, videoStream.Codec) switch
        {
            (HardwareAccelerationMode.Vaapi, _) => new DecoderVaapi(),
            _ => GetSoftwareDecoder(videoStream)
        };

        foreach (IDecoder decoder in maybeDecoder)
        {
            videoInputFile.AddOption(decoder);
        }
    }

    protected override FilterChain SetVideoFilters(
        VideoInputFile videoInputFile,
        VideoStream videoStream,
        Option<WatermarkInputFile> watermarkInputFile,
        Option<SubtitleInputFile> subtitleInputFile,
        PipelineContext context,
        FFmpegState ffmpegState,
        FrameState desiredState,
        string fontsFolder,
        ICollection<IPipelineStep> pipelineSteps)
    {
        var watermarkOverlayFilterSteps = new List<IPipelineFilterStep>();
        var subtitleOverlayFilterSteps = new List<IPipelineFilterStep>();

        FrameState currentState = desiredState with
        {
            ScaledSize = videoStream.FrameSize,
            PaddedSize = videoStream.FrameSize,

            PixelFormat = videoStream.PixelFormat,

            IsAnamorphic = videoStream.IsAnamorphic,

            FrameDataLocation = ffmpegState.DecoderHardwareAccelerationMode == HardwareAccelerationMode.Vaapi
                ? FrameDataLocation.Hardware
                : FrameDataLocation.Software
        };

        // easier to use nv12 for overlay
        if (context.HasSubtitleOverlay || context.HasWatermark)
        {
            IPixelFormat pixelFormat = desiredState.PixelFormat.IfNone(
                context.Is10BitOutput ? new PixelFormatYuv420P10Le() : new PixelFormatYuv420P());
            desiredState = desiredState with { PixelFormat = new PixelFormatNv12(pixelFormat.Name) };
        }

        // _logger.LogDebug("After decode: {PixelFormat}", currentState.PixelFormat);

        currentState = SetDeinterlace(videoInputFile, context, ffmpegState, currentState);
        // _logger.LogDebug("After deinterlace: {PixelFormat}", currentState.PixelFormat);

        currentState = SetScale(videoInputFile, videoStream, context, ffmpegState, desiredState, currentState);
        // _logger.LogDebug("After scale: {PixelFormat}", currentState.PixelFormat);

        currentState = SetPad(videoInputFile, videoStream, desiredState, currentState);
        // _logger.LogDebug("After pad: {PixelFormat}", currentState.PixelFormat);

        // need to upload for hardware overlay
        bool forceSoftwareOverlay = context.HasSubtitleOverlay && context.HasWatermark;

        if (currentState.FrameDataLocation == FrameDataLocation.Software && context.HasSubtitleOverlay &&
            !forceSoftwareOverlay)
        {
            var hardwareUpload = new HardwareUploadVaapiFilter(true);
            currentState = hardwareUpload.NextState(currentState);
            videoInputFile.FilterSteps.Add(hardwareUpload);
        }
        else if(currentState.FrameDataLocation == FrameDataLocation.Hardware &&
                (!context.HasSubtitleOverlay || forceSoftwareOverlay) &&
                context.HasWatermark)
        {
            // download for watermark (or forced software subtitle)
            var hardwareDownload = new HardwareDownloadFilter(currentState);
            currentState = hardwareDownload.NextState(currentState);
            videoInputFile.FilterSteps.Add(hardwareDownload);
        }

        currentState = SetSubtitle(
            videoInputFile,
            subtitleInputFile,
            context,
            forceSoftwareOverlay,
            ffmpegState,
            currentState,
            desiredState,
            fontsFolder,
            subtitleOverlayFilterSteps);
        
        currentState = SetWatermark(
            videoStream,
            watermarkInputFile,
            context,
            ffmpegState,
            desiredState,
            currentState,
            watermarkOverlayFilterSteps);

        // after everything else is done, apply the encoder
        if (pipelineSteps.OfType<IEncoder>().All(e => e.Kind != StreamKind.Video))
        {
            Option<IEncoder> maybeEncoder =
                (ffmpegState.EncoderHardwareAccelerationMode, desiredState.VideoFormat) switch
                {
                    (HardwareAccelerationMode.Vaapi, VideoFormat.Hevc) => new EncoderHevcVaapi(),
                    (HardwareAccelerationMode.Vaapi, VideoFormat.H264) => new EncoderH264Vaapi(),

                    (_, _) => GetSoftwareEncoder(currentState, desiredState)
                };

            foreach (IEncoder encoder in maybeEncoder)
            {
                pipelineSteps.Add(encoder);
                videoInputFile.FilterSteps.Add(encoder);
            }
        }
        
        List<IPipelineFilterStep> pixelFormatFilterSteps = SetPixelFormat(
            videoStream,
            desiredState.PixelFormat,
            ffmpegState,
            currentState,
            context,
            pipelineSteps);

        return new FilterChain(
            videoInputFile.FilterSteps,
            watermarkInputFile.Map(wm => wm.FilterSteps).IfNone(new List<IPipelineFilterStep>()),
            subtitleInputFile.Map(st => st.FilterSteps).IfNone(new List<IPipelineFilterStep>()),
            watermarkOverlayFilterSteps,
            subtitleOverlayFilterSteps,
            pixelFormatFilterSteps);
    }
    
    private List<IPipelineFilterStep> SetPixelFormat(
        VideoStream videoStream,
        Option<IPixelFormat> desiredPixelFormat,
        FFmpegState ffmpegState,
        FrameState currentState,
        PipelineContext context,
        ICollection<IPipelineStep> pipelineSteps)
    {
        var result = new List<IPipelineFilterStep>();

        foreach (IPixelFormat pixelFormat in desiredPixelFormat)
        {
            IPixelFormat format = pixelFormat;

            if (pixelFormat is PixelFormatNv12 nv12)
            {
                foreach (IPixelFormat pf in AvailablePixelFormats.ForPixelFormat(nv12.Name, null))
                {
                    format = pf;
                }
            }

            if (!videoStream.ColorParams.IsBt709)
            {
                _logger.LogDebug("Adding colorspace filter");
                var colorspace = new ColorspaceFilter(videoStream, format, false, currentState.FrameDataLocation);
                currentState = colorspace.NextState(currentState);
                result.Add(colorspace);
            }

            if (ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.None)
            {
                _logger.LogDebug("Using software encoder");
                
                if (currentState.FrameDataLocation == FrameDataLocation.Hardware)
                {
                    _logger.LogDebug("FrameDataLocation == FrameDataLocation.Hardware");

                    var hardwareDownload =
                        new HardwareDownloadFilter(currentState with { PixelFormat = Some(format) });
                    currentState = hardwareDownload.NextState(currentState);
                    result.Add(hardwareDownload);
                }
            }

            if (currentState.PixelFormat.Map(f => f.FFmpegName) != format.FFmpegName)
            {
                _logger.LogDebug(
                    "Format {A} doesn't equal {B}",
                    currentState.PixelFormat.Map(f => f.FFmpegName),
                    format.FFmpegName);

                // NV12 is 8-bit
                if (format is PixelFormatYuv420P)
                {
                    format = new PixelFormatNv12(format.Name);
                }

                if (currentState.FrameDataLocation == FrameDataLocation.Hardware)
                {
                    result.Add(new VaapiFormatFilter(format));
                }
                else
                {
                    result.Add(new PixelFormatFilter(format));
                }
            }

            if (ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.Vaapi &&
                currentState.FrameDataLocation == FrameDataLocation.Software)
            {
                bool setFormat = result.All(f => f is not VaapiFormatFilter && f is not PixelFormatFilter);
                result.Add(new HardwareUploadVaapiFilter(setFormat));
            }
        }

        return result;
    }

    private FrameState SetWatermark(
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

            watermark.FilterSteps.Add(new PixelFormatFilter(new PixelFormatYuva420P()));

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

        return currentState;
    }

    private static FrameState SetSubtitle(
        VideoInputFile videoInputFile,
        Option<SubtitleInputFile> subtitleInputFile,
        PipelineContext context,
        bool forceSoftwareOverlay,
        FFmpegState ffmpegState,
        FrameState currentState,
        FrameState desiredState,
        string fontsFolder,
        ICollection<IPipelineFilterStep> subtitleOverlayFilterSteps)
    {
        foreach (SubtitleInputFile subtitle in subtitleInputFile)
        {
            if (context.HasSubtitleText)
            {
                videoInputFile.AddOption(new CopyTimestampInputOption());

                // if (videoInputFile.FilterSteps.Count == 0 && videoInputFile.InputOptions.OfType<CuvidDecoder>().Any())
                // {
                //     // change the hw accel output to software so the explicit download isn't needed
                //     foreach (CuvidDecoder decoder in videoInputFile.InputOptions.OfType<CuvidDecoder>())
                //     {
                //         decoder.HardwareAccelerationMode = HardwareAccelerationMode.None;
                //     }
                // }
                // else
                // {
                    var downloadFilter = new HardwareDownloadFilter(currentState);
                    currentState = downloadFilter.NextState(currentState);
                    videoInputFile.FilterSteps.Add(downloadFilter);
                // }

                var subtitlesFilter = new SubtitlesFilter(fontsFolder, subtitle);
                currentState = subtitlesFilter.NextState(currentState);
                videoInputFile.FilterSteps.Add(subtitlesFilter);
            }
            else if (context.HasSubtitleOverlay)
            {
                var pixelFormatFilter = new PixelFormatFilter(new PixelFormatArgb());
                subtitle.FilterSteps.Add(pixelFormatFilter);

                if (forceSoftwareOverlay)
                {
                    foreach (IPixelFormat pixelFormat in desiredState.PixelFormat)
                    {
                        IPixelFormat pf = pixelFormat;
                        if (pixelFormat is PixelFormatNv12 nv12)
                        {
                            foreach (IPixelFormat format in AvailablePixelFormats.ForPixelFormat(nv12.Name, null))
                            {
                                pf = format;
                            }
                        }
                        
                        var subtitlesFilter = new OverlaySubtitleFilter(pf);
                        subtitleOverlayFilterSteps.Add(subtitlesFilter);
                    }
                }
                else
                {
                    var subtitleHardwareUpload = new HardwareUploadVaapiFilter(false);
                    subtitle.FilterSteps.Add(subtitleHardwareUpload);

                    // always scale - shouldn't really be needed outside of transcoding tests, which use picture subtitles
                    // that are too big
                    var scaleFilter = new SubtitleScaleVaapiFilter(desiredState.PaddedSize);
                    subtitle.FilterSteps.Add(scaleFilter);

                    var subtitlesFilter = new OverlaySubtitleVaapiFilter();
                    subtitleOverlayFilterSteps.Add(subtitlesFilter);
                }

                if (context.HasWatermark && !forceSoftwareOverlay)
                {
                    // download for watermark
                    var hardwareDownload = new HardwareDownloadFilter(currentState);
                    currentState = hardwareDownload.NextState(currentState);
                    subtitleOverlayFilterSteps.Add(hardwareDownload);
                }
            }
        }

        return currentState;
    }

    private static FrameState SetPad(
        VideoInputFile videoInputFile,
        VideoStream videoStream,
        FrameState desiredState,
        FrameState currentState)
    {
        if (currentState.PaddedSize != desiredState.PaddedSize)
        {
            IPipelineFilterStep padStep = new PadFilter(
                currentState,
                desiredState.PaddedSize);
            currentState = padStep.NextState(currentState);
            videoInputFile.FilterSteps.Add(padStep);
        }
        
        return currentState;
    }

    private static FrameState SetScale(
        VideoInputFile videoInputFile,
        VideoStream videoStream,
        PipelineContext context,
        FFmpegState ffmpegState,
        FrameState desiredState,
        FrameState currentState)
    {
        IPipelineFilterStep scaleStep;
        
        if ((currentState.ScaledSize != desiredState.ScaledSize && ffmpegState is
            {
                DecoderHardwareAccelerationMode: HardwareAccelerationMode.None,
                EncoderHardwareAccelerationMode: HardwareAccelerationMode.None
            } && context is { HasWatermark: false, HasSubtitleOverlay: false, ShouldDeinterlace: false }) ||
            ffmpegState.DecoderHardwareAccelerationMode != HardwareAccelerationMode.Vaapi)
        {
            scaleStep = new ScaleFilter(
                currentState,
                desiredState.ScaledSize,
                desiredState.PaddedSize,
                videoStream.IsAnamorphicEdgeCase);
        }
        else
        {
            scaleStep = new ScaleVaapiFilter(
                currentState with
                {
                    PixelFormat = //context.HasWatermark ||
                                  //context.HasSubtitleOverlay ||
                                  // (desiredState.ScaledSize != desiredState.PaddedSize) ||
                                  // context.HasSubtitleText ||
                                  ffmpegState is
                                  {
                                      DecoderHardwareAccelerationMode: HardwareAccelerationMode.Nvenc,
                                      EncoderHardwareAccelerationMode: HardwareAccelerationMode.None
                                  }
                        ? desiredState.PixelFormat.Map(pf => (IPixelFormat)new PixelFormatNv12(pf.Name))
                        : Option<IPixelFormat>.None
                },
                desiredState.ScaledSize,
                desiredState.PaddedSize,
                videoStream.IsAnamorphicEdgeCase);
        }

        if (!string.IsNullOrWhiteSpace(scaleStep.Filter))
        {
            currentState = scaleStep.NextState(currentState);
            videoInputFile.FilterSteps.Add(scaleStep);
        }

        return currentState;
    }

    private static FrameState SetDeinterlace(
        VideoInputFile videoInputFile,
        PipelineContext context,
        FFmpegState ffmpegState,
        FrameState currentState)
    {
        if (context.ShouldDeinterlace)
        {
            if (ffmpegState.DecoderHardwareAccelerationMode == HardwareAccelerationMode.Vaapi)
            {
                var filter = new DeinterlaceVaapiFilter(currentState);
                currentState = filter.NextState(currentState);
                videoInputFile.FilterSteps.Add(filter);
            }
            else
            {
                var filter = new YadifFilter(currentState);
                currentState = filter.NextState(currentState);
                videoInputFile.FilterSteps.Add(filter);
            }
        }

        return currentState;
    }
}
