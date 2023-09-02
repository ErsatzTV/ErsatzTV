using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.FFmpeg.Decoder;
using ErsatzTV.FFmpeg.Encoder;
using ErsatzTV.FFmpeg.Encoder.VideoToolbox;
using ErsatzTV.FFmpeg.Filter;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.GlobalOption.HardwareAcceleration;
using ErsatzTV.FFmpeg.OutputOption;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Pipeline;

public class VideoToolboxPipelineBuilder : SoftwarePipelineBuilder
{
    private readonly IHardwareCapabilities _hardwareCapabilities;
    private readonly ILogger _logger;

    public VideoToolboxPipelineBuilder(
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

        pipelineSteps.Add(new VideoToolboxHardwareAccelerationOption());

        // disable hw accel if decoder/encoder isn't supported
        return ffmpegState with
        {
            DecoderHardwareAccelerationMode = decodeCapability == FFmpegCapability.Hardware
                ? HardwareAccelerationMode.VideoToolbox
                : HardwareAccelerationMode.None,
            EncoderHardwareAccelerationMode = encodeCapability == FFmpegCapability.Hardware
                ? HardwareAccelerationMode.VideoToolbox
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
            (HardwareAccelerationMode.VideoToolbox, _) => new DecoderVideoToolbox(),

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
            (HardwareAccelerationMode.VideoToolbox, VideoFormat.Hevc) =>
                new EncoderHevcVideoToolbox(desiredState.BitDepth),
            (HardwareAccelerationMode.VideoToolbox, VideoFormat.H264) =>
                new EncoderH264VideoToolbox(),

            _ => GetSoftwareEncoder(currentState, desiredState)
        };

    protected override List<IPipelineFilterStep> SetPixelFormat(
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

                //result.Add(new PixelFormatFilter(pixelFormat));
            }

            pipelineSteps.Add(new PixelFormatOutputOption(pixelFormat));
        }

        return result;
    }
}
