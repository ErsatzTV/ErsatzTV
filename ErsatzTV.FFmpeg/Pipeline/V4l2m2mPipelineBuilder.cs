using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.FFmpeg.Decoder;
using ErsatzTV.FFmpeg.Decoder.V4l2m2m;
using ErsatzTV.FFmpeg.Encoder;
using ErsatzTV.FFmpeg.Encoder.V4l2m2m;
using ErsatzTV.FFmpeg.Filter;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.OutputFormat;
using ErsatzTV.FFmpeg.OutputOption;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Pipeline;

public class V4l2m2mPipelineBuilder : SoftwarePipelineBuilder
{
    private readonly IHardwareCapabilities _hardwareCapabilities;
    private readonly ILogger _logger;

    public V4l2m2mPipelineBuilder(
        IFFmpegCapabilities ffmpegCapabilities,
        IHardwareCapabilities hardwareCapabilities,
        HardwareAccelerationMode hardwareAccelerationMode,
        Option<VideoInputFile> videoInputFile,
        Option<AudioInputFile> audioInputFile,
        Option<WatermarkInputFile> watermarkInputFile,
        Option<SubtitleInputFile> subtitleInputFile,
        Option<ConcatInputFile> concatInputFile,
        Option<GraphicsEngineInput> graphicsEngineInput,
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
        graphicsEngineInput,
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
            videoStream.Profile,
            videoStream.PixelFormat,
            videoStream.ColorParams);
        FFmpegCapability encodeCapability = _hardwareCapabilities.CanEncode(
            desiredState.VideoFormat,
            desiredState.VideoProfile,
            desiredState.PixelFormat);

        // use software encoding (rawvideo) when piping to parent hls segmenter
        if (ffmpegState.OutputFormat is OutputFormatKind.Nut)
        {
            encodeCapability = FFmpegCapability.Software;
        }

        // disable hw accel if decoder/encoder isn't supported
        return ffmpegState with
        {
            DecoderHardwareAccelerationMode = decodeCapability == FFmpegCapability.Hardware
                ? HardwareAccelerationMode.V4l2m2m
                : HardwareAccelerationMode.None,
            EncoderHardwareAccelerationMode = encodeCapability == FFmpegCapability.Hardware
                ? HardwareAccelerationMode.V4l2m2m
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
            (HardwareAccelerationMode.V4l2m2m, VideoFormat.Hevc) => new DecoderHevcV4l2m2m(),
            (HardwareAccelerationMode.V4l2m2m, VideoFormat.H264) => new DecoderH264V4l2m2m(),
            (HardwareAccelerationMode.V4l2m2m, VideoFormat.Mpeg2Video) => new DecoderMpeg2V4l2m2m(),
            (HardwareAccelerationMode.V4l2m2m, VideoFormat.Mpeg4) => new DecoderMpeg4V4l2m2m(),
            (HardwareAccelerationMode.V4l2m2m, VideoFormat.Vc1) => new DecoderVc1V4l2m2m(),
            (HardwareAccelerationMode.V4l2m2m, VideoFormat.Vp8) => new DecoderVp8V4l2m2m(),
            (HardwareAccelerationMode.V4l2m2m, VideoFormat.Vp9) => new DecoderVp9V4l2m2m(),

            _ => GetSoftwareDecoder(videoStream)
        };

        foreach (IDecoder decoder in maybeDecoder)
        {
            videoInputFile.AddOption(decoder);
            return Some(decoder);
        }

        return None;
    }

    protected override Option<IEncoder> GetEncoder(
        FFmpegState ffmpegState,
        FrameState currentState,
        FrameState desiredState) =>
        (ffmpegState.EncoderHardwareAccelerationMode, desiredState.VideoFormat) switch
        {
            (HardwareAccelerationMode.V4l2m2m, VideoFormat.Hevc) =>
                new EncoderHevcV4l2m2m(),
            (HardwareAccelerationMode.V4l2m2m, VideoFormat.H264) =>
                new EncoderH264V4l2m2m(),

            _ => GetSoftwareEncoder(ffmpegState, currentState, desiredState)
        };

    protected override List<IPipelineFilterStep> SetPixelFormat(
        VideoStream videoStream,
        FrameState desiredState,
        FrameState currentState,
        ICollection<IPipelineStep> pipelineSteps)
    {
        var result = new List<IPipelineFilterStep>();

        foreach (IPixelFormat pixelFormat in desiredState.PixelFormat)
        {
            if (desiredState.ColorsAreBt709 && !videoStream.ColorParams.IsBt709)
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

                //result.Add(new PixelFormatFilter(pixelFormat));
            }

            pipelineSteps.Add(new PixelFormatOutputOption(pixelFormat));
        }

        return result;
    }
}
