using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.FFmpeg.Decoder;
using ErsatzTV.FFmpeg.Decoder.Qsv;
using ErsatzTV.FFmpeg.Encoder;
using ErsatzTV.FFmpeg.Encoder.Qsv;
using ErsatzTV.FFmpeg.Filter;
using ErsatzTV.FFmpeg.Filter.Qsv;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.Option;
using ErsatzTV.FFmpeg.Option.HardwareAcceleration;
using ErsatzTV.FFmpeg.State;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Pipeline;

public class QsvPipelineBuilder : SoftwarePipelineBuilder
{
    private readonly IHardwareCapabilities _hardwareCapabilities;
    private readonly ILogger _logger;

    public QsvPipelineBuilder(
        IFFmpegCapabilities ffmpegCapabilities,
        IHardwareCapabilities hardwareCapabilities,
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
        FFmpegCapability decodeCapability = _hardwareCapabilities.CanDecode(
            videoStream.Codec,
            desiredState.VideoProfile,
            videoStream.PixelFormat);
        FFmpegCapability encodeCapability = _hardwareCapabilities.CanEncode(
            desiredState.VideoFormat,
            desiredState.VideoProfile,
            desiredState.PixelFormat);

        pipelineSteps.Add(new QsvHardwareAccelerationOption(ffmpegState.VaapiDevice));

        bool isHevcOrH264 = videoStream.Codec is VideoFormat.Hevc or VideoFormat.H264;
        bool is10Bit = videoStream.PixelFormat.Map(pf => pf.BitDepth).IfNone(8) == 10;
        
        // 10-bit hevc/h264 qsv decoders have issues, so use software
        if (decodeCapability == FFmpegCapability.Hardware && isHevcOrH264 && is10Bit)
        {
            decodeCapability = FFmpegCapability.Software;
        }

        // disable hw accel if decoder/encoder isn't supported
        return ffmpegState with
        {
            DecoderHardwareAccelerationMode = decodeCapability == FFmpegCapability.Hardware
                ? HardwareAccelerationMode.Qsv
                : HardwareAccelerationMode.None,
            EncoderHardwareAccelerationMode = encodeCapability == FFmpegCapability.Hardware
                ? HardwareAccelerationMode.Qsv
                : HardwareAccelerationMode.None
        };
    }

    protected override Option<IDecoder> SetDecoder(
        VideoInputFile videoInputFile,
        VideoStream videoStream,
        FFmpegState ffmpegState,
        PipelineContext context)
    {
        Option<IDecoder> maybeDecoder = (ffmpegState.DecoderHardwareAccelerationMode, videoStream.Codec) switch
        {
            (HardwareAccelerationMode.Qsv, VideoFormat.Hevc) => new DecoderHevcQsv(),
            (HardwareAccelerationMode.Qsv, VideoFormat.H264) => new DecoderH264Qsv(),
            (HardwareAccelerationMode.Qsv, VideoFormat.Mpeg2Video) => new DecoderMpeg2Qsv(),
            (HardwareAccelerationMode.Qsv, VideoFormat.Vc1) => new DecoderVc1Qsv(),
            (HardwareAccelerationMode.Qsv, VideoFormat.Vp9) => new DecoderVp9Qsv(),
            (HardwareAccelerationMode.Qsv, VideoFormat.Av1) => new DecoderAv1Qsv(),

            _ => GetSoftwareDecoder(videoStream)
        };

        foreach (IDecoder decoder in maybeDecoder)
        {
            videoInputFile.AddOption(decoder);
            return Some(decoder);
        }

        return None;
    }

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
            ScaledSize = videoStream.FrameSize,
            PaddedSize = videoStream.FrameSize,
            
            // consider 8-bit hardware frames to be wrapped in nv12
            PixelFormat = ffmpegState.DecoderHardwareAccelerationMode == HardwareAccelerationMode.Qsv
                ? videoStream.PixelFormat.Map(pf => pf.BitDepth == 8 ? new PixelFormatNv12(pf.Name) : pf)
                : videoStream.PixelFormat,
            
            IsAnamorphic = videoStream.IsAnamorphic
        };
        
        foreach (IDecoder decoder in maybeDecoder)
        {
            currentState = decoder.NextState(currentState);
        }

        // easier to use nv12 for overlay
        if (context.HasSubtitleOverlay || context.HasWatermark)
        {
            IPixelFormat pixelFormat = desiredState.PixelFormat.IfNone(
                context.Is10BitOutput ? new PixelFormatYuv420P10Le() : new PixelFormatYuv420P());
            desiredState = desiredState with
            {
                PixelFormat = Some(context.Is10BitOutput ? pixelFormat : new PixelFormatNv12(pixelFormat.Name))
            };
        }

        // _logger.LogDebug("After decode: {PixelFormat}", currentState.PixelFormat);
        currentState = SetDeinterlace(videoInputFile, context, ffmpegState, currentState);
        // _logger.LogDebug("After deinterlace: {PixelFormat}", currentState.PixelFormat);
        currentState = SetScale(videoInputFile, videoStream, context, ffmpegState, desiredState, currentState);
        // _logger.LogDebug("After scale: {PixelFormat}", currentState.PixelFormat);
        currentState = SetPad(videoInputFile, videoStream, desiredState, currentState);
        // _logger.LogDebug("After pad: {PixelFormat}", currentState.PixelFormat);

        // need to download for any sort of overlay
        if (currentState.FrameDataLocation == FrameDataLocation.Hardware &&
            (context.HasSubtitleOverlay || context.HasWatermark))
        {
            var hardwareDownload = new HardwareDownloadFilter(currentState);
            currentState = hardwareDownload.NextState(currentState);
            videoInputFile.FilterSteps.Add(hardwareDownload);
        }
        
        currentState = SetSubtitle(
            videoInputFile,
            subtitleInputFile,
            context,
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
                    (HardwareAccelerationMode.Qsv, VideoFormat.Hevc) => new EncoderHevcQsv(),
                    (HardwareAccelerationMode.Qsv, VideoFormat.H264) => new EncoderH264Qsv(),
                    (HardwareAccelerationMode.Qsv, VideoFormat.Mpeg2Video) => new EncoderMpeg2Qsv(),

                    (_, _) => GetSoftwareEncoder(currentState, desiredState)
                };

            foreach (IEncoder encoder in maybeEncoder)
            {
                pipelineSteps.Add(encoder);
                videoInputFile.FilterSteps.Add(encoder);
            }
        }
        
        List<IPipelineFilterStep> pixelFormatFilterSteps = SetPixelFormat(
            videoInputFile,
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
        VideoInputFile videoInputFile,
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

            IPixelFormat formatForDownload = pixelFormat;

            bool usesVppQsv =
                videoInputFile.FilterSteps.Any(f => f is QsvFormatFilter or ScaleQsvFilter or DeinterlaceQsvFilter);

            // if we have no filters, check whether we need to convert pixel format
            // since qsv doesn't seem to like doing that at the encoder
            if (!videoInputFile.FilterSteps.Any(f => f is not IEncoder))
            {
                foreach (IPixelFormat currentPixelFormat in currentState.PixelFormat)
                {
                    bool requiresConversion = false;
                    
                    if (currentPixelFormat is PixelFormatNv12 nv)
                    {
                        foreach (IPixelFormat pf in AvailablePixelFormats.ForPixelFormat(nv.Name, null))
                        {
                            requiresConversion = pf.FFmpegName != format.FFmpegName;

                            if (!requiresConversion)
                            {
                                currentState = currentState with { PixelFormat = Some(pf) };
                            }
                        }
                    }
                    else
                    {
                        requiresConversion = currentPixelFormat.FFmpegName != format.FFmpegName;
                    }

                    if (requiresConversion)
                    {
                        if (currentState.FrameDataLocation == FrameDataLocation.Hardware)
                        {
                            var filter = new QsvFormatFilter(currentPixelFormat);
                            result.Add(filter);
                            currentState = filter.NextState(currentState);
                            
                            // if we need to convert 8-bit to 10-bit, do it here
                            if (currentPixelFormat.BitDepth == 8 && context.Is10BitOutput)
                            {
                                var p010Filter = new QsvFormatFilter(new PixelFormatP010());
                                result.Add(p010Filter);
                                currentState = p010Filter.NextState(currentState);
                            }

                            usesVppQsv = true;
                        }
                    }
                }
            }

            if (!videoStream.ColorParams.IsBt709 || usesVppQsv)
            {
                // _logger.LogDebug("Adding colorspace filter");

                // force p010/nv12 if we're still in hardware
                if (currentState.FrameDataLocation == FrameDataLocation.Hardware)
                {
                    foreach (int bitDepth in currentState.PixelFormat.Map(pf => pf.BitDepth))
                    {
                        if (bitDepth is 10 && formatForDownload is not PixelFormatYuv420P10Le)
                        {
                            formatForDownload = new PixelFormatYuv420P10Le();
                            currentState = currentState with { PixelFormat = Some(formatForDownload) };
                        }
                        else if (bitDepth is 8 && formatForDownload is not PixelFormatNv12)
                        {
                            formatForDownload = new PixelFormatNv12(formatForDownload.Name);
                            currentState = currentState with { PixelFormat = Some(formatForDownload) };
                        }
                    }
                }

                // vpp_qsv seems to strip color info, so if we use that at all, force overriding input color info
                var colorspace = new ColorspaceFilter(
                    currentState,
                    videoStream,
                    format,
                    forceInputOverrides: usesVppQsv);

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
                        new HardwareDownloadFilter(currentState with { PixelFormat = Some(formatForDownload) });
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

                // remind qsv that it uses qsv
                if (currentState.FrameDataLocation == FrameDataLocation.Hardware &&
                    result is [ColorspaceFilter colorspace])
                {
                    if (colorspace.Filter.StartsWith("setparams="))
                    {
                        result.Insert(0, new QsvFormatFilter(new PixelFormatQsv(format.Name)));
                    }
                }

                pipelineSteps.Add(new PixelFormatOutputOption(format));
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

            IPixelFormat pixelFormat = context.Is10BitOutput
                ? new PixelFormatNv12(FFmpegFormat.P010LE)
                : new PixelFormatNv12(FFmpegFormat.YUVA420P);

            watermark.FilterSteps.Add(new PixelFormatFilter(pixelFormat));

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

                // overlay filter with 10-bit vp9 seems to output alpha channel, so remove it with a pixel format change
                if (videoStream.Codec == "vp9" && desiredPixelFormat.BitDepth == 10)
                {
                    watermarkOverlayFilterSteps.Add(new PixelFormatFilter(new PixelFormatYuv420P10Le()));
                }
            }
        }

        return currentState;
    }

    private static FrameState SetSubtitle(
        VideoInputFile videoInputFile,
        Option<SubtitleInputFile> subtitleInputFile,
        PipelineContext context,
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

                var downloadFilter = new HardwareDownloadFilter(currentState);
                currentState = downloadFilter.NextState(currentState);
                videoInputFile.FilterSteps.Add(downloadFilter);

                var subtitlesFilter = new SubtitlesFilter(fontsFolder, subtitle);
                currentState = subtitlesFilter.NextState(currentState);
                videoInputFile.FilterSteps.Add(subtitlesFilter);
            }
            else if (context.HasSubtitleOverlay)
            {
                IPixelFormat pixelFormat = new PixelFormatYuva420P();

                var pixelFormatFilter = new PixelFormatFilter(pixelFormat);
                subtitle.FilterSteps.Add(pixelFormatFilter);

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
                    
                    var subtitlesFilter = new OverlaySubtitleFilter(pf);
                    subtitleOverlayFilterSteps.Add(subtitlesFilter);

                    // overlay filter with 10-bit vp9 seems to output alpha channel, so remove it with a pixel format change
                    if (videoInputFile.VideoStreams.Any(vs => vs.Codec == "vp9") && context.Is10BitOutput)
                    {
                        subtitleOverlayFilterSteps.Add(new PixelFormatFilter(new PixelFormatYuv420P10Le()));
                    }
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
        
        if (currentState.ScaledSize != desiredState.ScaledSize && ffmpegState is
            {
                DecoderHardwareAccelerationMode: HardwareAccelerationMode.None,
                EncoderHardwareAccelerationMode: HardwareAccelerationMode.None
            } && context is { HasWatermark: false, HasSubtitleOverlay: false, ShouldDeinterlace: false })
        {
            scaleStep = new ScaleFilter(
                currentState,
                desiredState.ScaledSize,
                desiredState.PaddedSize,
                videoStream.IsAnamorphicEdgeCase);
        }
        else
        {
            scaleStep = new ScaleQsvFilter(
                currentState with
                {
                    PixelFormat = ffmpegState is
                                  {
                                      DecoderHardwareAccelerationMode: HardwareAccelerationMode.Nvenc,
                                      EncoderHardwareAccelerationMode: HardwareAccelerationMode.None
                                  }
                        ? desiredState.PixelFormat.Map(pf => (IPixelFormat)new PixelFormatNv12(pf.Name))
                        : Option<IPixelFormat>.None
                },
                desiredState.ScaledSize,
                ffmpegState.QsvExtraHardwareFrames,
                videoStream.IsAnamorphicEdgeCase,
                videoStream.SampleAspectRatio);
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
            var filter = new DeinterlaceQsvFilter(currentState, ffmpegState.QsvExtraHardwareFrames);
            currentState = filter.NextState(currentState);
            videoInputFile.FilterSteps.Add(filter);
        }

        return currentState;
    }
}
