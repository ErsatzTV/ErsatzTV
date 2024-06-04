using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.FFmpeg.Decoder;
using ErsatzTV.FFmpeg.Decoder.Cuvid;
using ErsatzTV.FFmpeg.Encoder;
using ErsatzTV.FFmpeg.Encoder.Nvenc;
using ErsatzTV.FFmpeg.Filter;
using ErsatzTV.FFmpeg.Filter.Cuda;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.GlobalOption.HardwareAcceleration;
using ErsatzTV.FFmpeg.InputOption;
using ErsatzTV.FFmpeg.OutputFormat;
using ErsatzTV.FFmpeg.OutputOption;
using ErsatzTV.FFmpeg.State;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Pipeline;

public class NvidiaPipelineBuilder : SoftwarePipelineBuilder
{
    private readonly IFFmpegCapabilities _ffmpegCapabilities;
    private readonly IHardwareCapabilities _hardwareCapabilities;
    private readonly ILogger _logger;

    public NvidiaPipelineBuilder(
        IFFmpegCapabilities ffmpegCapabilities,
        IHardwareCapabilities hardwareCapabilities,
        HardwareAccelerationMode hardwareAccelerationMode,
        Option<VideoInputFile> videoInputFile,
        Option<AudioInputFile> audioInputFile,
        Option<WatermarkInputFile> watermarkInputFile,
        Option<SubtitleInputFile> subtitleInputFile,
        Option<ConcatInputFile> concatInputFile,
        string reportsFolder,
        string fontsFolder,
        ILogger logger) : base(
        ffmpegCapabilities,
        hardwareAccelerationMode,
        videoInputFile,
        audioInputFile,
        watermarkInputFile,
        subtitleInputFile,
        concatInputFile,
        reportsFolder,
        fontsFolder,
        logger)
    {
        _ffmpegCapabilities = ffmpegCapabilities;
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
            videoStream.Profile,
            videoStream.PixelFormat);
        FFmpegCapability encodeCapability = _hardwareCapabilities.CanEncode(
            desiredState.VideoFormat,
            desiredState.VideoProfile,
            desiredState.PixelFormat);

        // use software encoding (rawvideo) when piping to parent hls segmenter
        if (ffmpegState.OutputFormat is OutputFormatKind.Nut)
        {
            encodeCapability = FFmpegCapability.Software;
        }

        // mpeg2_cuvid seems to have issues when yadif_cuda is used, so just use software decoding
        if (context.ShouldDeinterlace && videoStream.Codec == VideoFormat.Mpeg2Video)
        {
            decodeCapability = FFmpegCapability.Software;
        }

        if (decodeCapability == FFmpegCapability.Hardware || encodeCapability == FFmpegCapability.Hardware)
        {
            pipelineSteps.Add(new CudaHardwareAccelerationOption());
        }

        // disable hw accel if decoder/encoder isn't supported
        return ffmpegState with
        {
            DecoderHardwareAccelerationMode = decodeCapability == FFmpegCapability.Hardware
                ? HardwareAccelerationMode.Nvenc
                : HardwareAccelerationMode.None,
            EncoderHardwareAccelerationMode = encodeCapability == FFmpegCapability.Hardware
                ? HardwareAccelerationMode.Nvenc
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
            (HardwareAccelerationMode.Nvenc, VideoFormat.Hevc) => new DecoderHevcCuvid(HardwareAccelerationMode.Nvenc),
            (HardwareAccelerationMode.Nvenc, VideoFormat.H264) => new DecoderH264Cuvid(HardwareAccelerationMode.Nvenc),
            (HardwareAccelerationMode.Nvenc, VideoFormat.Mpeg2Video) => new DecoderMpeg2Cuvid(
                HardwareAccelerationMode.Nvenc,
                context.ShouldDeinterlace),
            (HardwareAccelerationMode.Nvenc, VideoFormat.Vc1) => new DecoderVc1Cuvid(HardwareAccelerationMode.Nvenc),
            (HardwareAccelerationMode.Nvenc, VideoFormat.Vp9) => new DecoderVp9Cuvid(HardwareAccelerationMode.Nvenc),
            (HardwareAccelerationMode.Nvenc, VideoFormat.Mpeg4) =>
                new DecoderMpeg4Cuvid(HardwareAccelerationMode.Nvenc),
            (HardwareAccelerationMode.Nvenc, VideoFormat.Av1) =>
                new DecoderAv1Cuvid(HardwareAccelerationMode.Nvenc),

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

            PixelFormat = ffmpegState.DecoderHardwareAccelerationMode == HardwareAccelerationMode.Nvenc &&
                          videoStream.BitDepth == 8
                ? videoStream.PixelFormat.Map(pf => (IPixelFormat)new PixelFormatNv12(pf.Name))
                : videoStream.PixelFormat,

            IsAnamorphic = videoStream.IsAnamorphic
        };

        foreach (IDecoder decoder in maybeDecoder)
        {
            currentState = decoder.NextState(currentState);
        }

        // if (context.HasSubtitleOverlay || context.HasWatermark)
        // {
        //     IPixelFormat pixelFormat = desiredState.PixelFormat.IfNone(
        //         context.Is10BitOutput ? new PixelFormatYuv420P10Le() : new PixelFormatYuv420P());
        //     desiredState = desiredState with { PixelFormat = Some(pixelFormat) };
        // }

        currentState = SetDeinterlace(videoInputFile, context, currentState);
        currentState = SetScale(videoInputFile, videoStream, context, ffmpegState, desiredState, currentState);
        currentState = SetPad(videoInputFile, videoStream, desiredState, currentState);
        currentState = SetCrop(videoInputFile, desiredState, currentState);
        SetStillImageLoop(videoInputFile, videoStream, desiredState, pipelineSteps);

        if (currentState.BitDepth == 8 && context.HasSubtitleOverlay || context.HasWatermark)
        {
            Option<IPixelFormat> desiredPixelFormat = Some((IPixelFormat)new PixelFormatYuv420P());

            if (desiredPixelFormat != currentState.PixelFormat)
            {
                if (currentState.FrameDataLocation == FrameDataLocation.Software)
                {
                    foreach (IPixelFormat pixelFormat in desiredPixelFormat)
                    {
                        var filter = new PixelFormatFilter(pixelFormat);
                        currentState = filter.NextState(currentState);
                        videoInputFile.FilterSteps.Add(filter);
                    }
                }
                else
                {
                    foreach (IPixelFormat pixelFormat in desiredPixelFormat)
                    {
                        var filter = new ScaleCudaFilter(
                            currentState with { PixelFormat = Some(pixelFormat) },
                            currentState.ScaledSize,
                            currentState.PaddedSize,
                            Option<FrameSize>.None,
                            false);
                        currentState = filter.NextState(currentState);
                        videoInputFile.FilterSteps.Add(filter);
                    }
                }
            }
        }

        // need to upload for any sort of overlay
        if (currentState.FrameDataLocation == FrameDataLocation.Software &&
            currentState.BitDepth == 8 && context.HasSubtitleText == false
            && (context.HasSubtitleOverlay || context.HasWatermark))
        {
            var hardwareUpload = new HardwareUploadCudaFilter(currentState);
            currentState = hardwareUpload.NextState(currentState);
            videoInputFile.FilterSteps.Add(hardwareUpload);
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

        // need to use software overlay for watermark with fade points
        // because `-loop 1` seems to cause a green line at the bottom of the resulting video with overlay_cuda
        if (context.HasWatermark && watermarkInputFile
                .Map(wm => wm.DesiredState.MaybeFadePoints.Map(fp => fp.Count > 0).IfNone(false)).IfNone(false))
        {
            var hardwareDownload = new CudaHardwareDownloadFilter(currentState.PixelFormat, None);
            currentState = hardwareDownload.NextState(currentState);
            videoInputFile.FilterSteps.Add(hardwareDownload);
        }

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
                    (HardwareAccelerationMode.Nvenc, VideoFormat.Hevc) =>
                        new EncoderHevcNvenc(_hardwareCapabilities, desiredState.VideoPreset),
                    (HardwareAccelerationMode.Nvenc, VideoFormat.H264) =>
                        new EncoderH264Nvenc(desiredState.VideoProfile, desiredState.VideoPreset),

                    (_, _) => GetSoftwareEncoder(ffmpegState, currentState, desiredState)
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

            // vp9 seems to lose color metadata through the ffmpeg pipeline
            // clearing color params will force it to be re-added
            if (videoStream.Codec == "vp9")
            {
                videoStream = videoStream with { ColorParams = ColorParams.Unknown };
            }

            if (!videoStream.ColorParams.IsBt709)
            {
                // _logger.LogDebug("Adding colorspace filter");
                var colorspace = new ColorspaceFilter(currentState, videoStream, format);

                currentState = colorspace.NextState(currentState);
                result.Add(colorspace);
            }

            if (ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.None)
            {
                _logger.LogDebug("Using software encoder");

                if ((context.HasSubtitleOverlay || context.HasWatermark) &&
                    currentState.FrameDataLocation == FrameDataLocation.Hardware)
                {
                    _logger.LogDebug(
                        "HasSubtitleOverlay || HasWatermark && FrameDataLocation == FrameDataLocation.Hardware");

                    var hardwareDownload = new CudaHardwareDownloadFilter(currentState.PixelFormat, None);
                    currentState = hardwareDownload.NextState(currentState);
                    result.Add(hardwareDownload);
                }
            }

            // _logger.LogDebug(
            //     "{CurrentPixelFormat} => {DesiredPixelFormat}",
            //     currentState
            //         .PixelFormat,
            //     desiredPixelFormat);

            if (currentState.FrameDataLocation == FrameDataLocation.Hardware &&
                ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.None)
            {
                if (currentState.PixelFormat.Map(f => f.FFmpegName) != format.FFmpegName)
                {
                    _logger.LogDebug(
                        "Format {A} doesn't equal {B}",
                        currentState.PixelFormat.Map(f => f.FFmpegName),
                        format.FFmpegName);

                    var formatFilter = new CudaFormatFilter(format);
                    currentState = formatFilter.NextState(currentState);
                    result.Add(formatFilter);
                }

                var hardwareDownload = new CudaHardwareDownloadFilter(currentState.PixelFormat, Some(format));
                currentState = hardwareDownload.NextState(currentState);
                result.Add(hardwareDownload);
            }

            if (currentState.PixelFormat.Map(f => f.FFmpegName) != format.FFmpegName)
            {
                _logger.LogDebug(
                    "Format {A} doesn't equal {B}",
                    currentState.PixelFormat.Map(f => f.FFmpegName),
                    format.FFmpegName);

                if (currentState.FrameDataLocation == FrameDataLocation.Hardware)
                {
                    bool noPipelineFilters = !pipelineSteps
                        .Filter(ps => ps is not IEncoder)
                        .OfType<IPipelineFilterStep>().Any();
                    bool hasColorspace = result is [ColorspaceFilter];

                    bool softwareDecoder = ffmpegState.DecoderHardwareAccelerationMode == HardwareAccelerationMode.None;
                    bool hardwareDecoder = !softwareDecoder;
                    bool hardwareEncoder =
                        ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.Nvenc;

                    if (softwareDecoder || noPipelineFilters && hasColorspace ||
                        hardwareDecoder && hardwareEncoder && noPipelineFilters)
                    {
                        result.Add(new CudaFormatFilter(format));
                    }
                    else
                    {
                        pipelineSteps.Add(new PixelFormatOutputOption(format));
                    }
                }
                else
                {
                    pipelineSteps.Add(new PixelFormatOutputOption(format));
                }
            }

            if (ffmpegState.OutputFormat is OutputFormatKind.Nut && format.BitDepth == 10)
            {
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

            watermark.FilterSteps.Add(new PixelFormatFilter(new PixelFormatYuva420P()));

            foreach (List<WatermarkFadePoint> fadePoints in watermark.DesiredState.MaybeFadePoints)
            {
                watermark.FilterSteps.AddRange(fadePoints.Map(fp => new WatermarkFadeFilter(fp)));
            }

            // if we're in software, it's because we need to overlay in software (watermark with fade points - loop)
            if (currentState.FrameDataLocation is FrameDataLocation.Software)
            {
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

                    var watermarkFilter = new OverlayWatermarkFilter(
                        watermark.DesiredState,
                        desiredState.PaddedSize,
                        videoStream.SquarePixelFrameSize(currentState.PaddedSize),
                        pf,
                        _logger);
                    watermarkOverlayFilterSteps.Add(watermarkFilter);
                }
            }
            else
            {
                watermark.FilterSteps.Add(
                    new HardwareUploadCudaFilter(currentState with { FrameDataLocation = FrameDataLocation.Software }));

                var watermarkFilter = new OverlayWatermarkCudaFilter(
                    watermark.DesiredState,
                    desiredState.PaddedSize,
                    videoStream.SquarePixelFrameSize(currentState.PaddedSize),
                    _logger);
                watermarkOverlayFilterSteps.Add(watermarkFilter);
                currentState = watermarkFilter.NextState(currentState);
            }
        }

        return currentState;
    }

    private FrameState SetSubtitle(
        VideoInputFile videoInputFile,
        Option<SubtitleInputFile> subtitleInputFile,
        PipelineContext context,
        FFmpegState ffmpegState,
        FrameState currentState,
        FrameState desiredState,
        string fontsFolder,
        List<IPipelineFilterStep> subtitleOverlayFilterSteps)
    {
        foreach (SubtitleInputFile subtitle in subtitleInputFile)
        {
            if (context.HasSubtitleText)
            {
                videoInputFile.AddOption(new CopyTimestampInputOption());

                if (videoInputFile.FilterSteps.Count == 0 && videoInputFile.InputOptions.OfType<CuvidDecoder>().Any())
                {
                    // change the hw accel output to software so the explicit download isn't needed
                    foreach (CuvidDecoder decoder in videoInputFile.InputOptions.OfType<CuvidDecoder>())
                    {
                        decoder.HardwareAccelerationMode = HardwareAccelerationMode.None;
                    }
                }
                else
                {
                    var downloadFilter = new HardwareDownloadFilter(currentState);
                    currentState = downloadFilter.NextState(currentState);
                    videoInputFile.FilterSteps.Add(downloadFilter);
                }

                var subtitlesFilter = new SubtitlesFilter(fontsFolder, subtitle);
                currentState = subtitlesFilter.NextState(currentState);
                videoInputFile.FilterSteps.Add(subtitlesFilter);

                if (context.HasWatermark)
                {
                    var subtitleHardwareUpload = new HardwareUploadCudaFilter(currentState);
                    currentState = subtitleHardwareUpload.NextState(currentState);
                    videoInputFile.FilterSteps.Add(subtitleHardwareUpload);
                }
            }
            else if (context.HasSubtitleOverlay)
            {
                var pixelFormatFilter = new PixelFormatFilter(new PixelFormatYuva420P());
                subtitle.FilterSteps.Add(pixelFormatFilter);

                if (currentState.PixelFormat.Map(pf => pf.BitDepth).IfNone(8) == 8)
                {
                    if (_ffmpegCapabilities.HasFilter(FFmpegKnownFilter.ScaleNpp))
                    {
                        var subtitleHardwareUpload = new HardwareUploadCudaFilter(
                            currentState with { FrameDataLocation = FrameDataLocation.Software });
                        subtitle.FilterSteps.Add(subtitleHardwareUpload);

                        // only scale if scaling or padding was used for main video stream
                        if (videoInputFile.FilterSteps.Any(
                                s => s is ScaleFilter or ScaleCudaFilter { IsFormatOnly: false } or PadFilter))
                        {
                            var scaleFilter = new SubtitleScaleNppFilter(desiredState.PaddedSize);
                            subtitle.FilterSteps.Add(scaleFilter);
                        }
                    }
                    else
                    {
                        // only scale if scaling or padding was used for main video stream
                        if (videoInputFile.FilterSteps.Any(
                                s => s is ScaleFilter or ScaleCudaFilter { IsFormatOnly: false } or PadFilter))
                        {
                            var scaleFilter = new ScaleImageFilter(desiredState.PaddedSize);
                            subtitle.FilterSteps.Add(scaleFilter);
                        }

                        var subtitleHardwareUpload = new HardwareUploadCudaFilter(
                            currentState with { FrameDataLocation = FrameDataLocation.Software });
                        subtitle.FilterSteps.Add(subtitleHardwareUpload);
                    }

                    var subtitlesFilter = new OverlaySubtitleCudaFilter();
                    subtitleOverlayFilterSteps.Add(subtitlesFilter);
                }
                else
                {
                    if (currentState.FrameDataLocation == FrameDataLocation.Hardware)
                    {
                        var cudaDownload = new CudaHardwareDownloadFilter(currentState.PixelFormat, None);
                        currentState = cudaDownload.NextState(currentState);
                        videoInputFile.FilterSteps.Add(cudaDownload);
                    }

                    // only scale if scaling or padding was used for main video stream
                    if (videoInputFile.FilterSteps.Any(s => s is ScaleFilter or ScaleCudaFilter or PadFilter))
                    {
                        var scaleFilter = new ScaleImageFilter(desiredState.PaddedSize);
                        subtitle.FilterSteps.Add(scaleFilter);
                    }

                    var subtitlesFilter =
                        new OverlaySubtitleFilter(desiredState.PixelFormat.IfNone(new PixelFormatYuv420P()));
                    subtitleOverlayFilterSteps.Add(subtitlesFilter);

                    // overlay produces YUVA420P10, so we need to strip the alpha
                    if (currentState.BitDepth == 10)
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
        if (desiredState.CroppedSize.IsNone && currentState.PaddedSize != desiredState.PaddedSize)
        {
            var padStep = new PadFilter(currentState, desiredState.PaddedSize);
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

        bool needsToScale = currentState.ScaledSize != desiredState.ScaledSize;
        if (!needsToScale)
        {
            return currentState;
        }

        bool decodedToSoftware = ffmpegState.DecoderHardwareAccelerationMode == HardwareAccelerationMode.None;
        bool softwareEncoder = ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.None;
        bool noHardwareFilters = context is
            { HasWatermark: false, HasSubtitleOverlay: false, ShouldDeinterlace: false };
        bool needsToPad = currentState.PaddedSize != desiredState.PaddedSize;

        if (decodedToSoftware && (needsToPad || noHardwareFilters && softwareEncoder))
        {
            scaleStep = new ScaleFilter(
                currentState,
                desiredState.ScaledSize,
                desiredState.PaddedSize,
                desiredState.CroppedSize,
                VideoStream.IsAnamorphicEdgeCase);
        }
        else
        {
            scaleStep = new ScaleCudaFilter(
                currentState with
                {
                    PixelFormat = !context.Is10BitOutput && (context.HasWatermark ||
                                                             context.HasSubtitleOverlay ||
                                                             context.ShouldDeinterlace ||
                                                             desiredState.ScaledSize != desiredState.PaddedSize ||
                                                             context.HasSubtitleText ||
                                                             ffmpegState is
                                                             {
                                                                 DecoderHardwareAccelerationMode:
                                                                 HardwareAccelerationMode.Nvenc,
                                                                 EncoderHardwareAccelerationMode:
                                                                 HardwareAccelerationMode.None
                                                             })
                        ? desiredState.PixelFormat.Map(pf => (IPixelFormat)new PixelFormatNv12(pf.Name))
                        : Option<IPixelFormat>.None
                },
                desiredState.ScaledSize,
                desiredState.PaddedSize,
                desiredState.CroppedSize,
                VideoStream.IsAnamorphicEdgeCase);
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
        FrameState currentState)
    {
        if (context.ShouldDeinterlace)
        {
            if (currentState.FrameDataLocation == FrameDataLocation.Software)
            {
                var filter = new YadifFilter(currentState);
                currentState = filter.NextState(currentState);
                videoInputFile.FilterSteps.Add(filter);
            }
            else
            {
                var filter = new YadifCudaFilter(currentState);
                currentState = filter.NextState(currentState);
                videoInputFile.FilterSteps.Add(filter);
            }
        }

        return currentState;
    }
}
