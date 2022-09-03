using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.FFmpeg.Encoder.Amf;
using ErsatzTV.FFmpeg.Encoder.Nvenc;
using ErsatzTV.FFmpeg.Encoder.Qsv;
using ErsatzTV.FFmpeg.Encoder.Vaapi;
using ErsatzTV.FFmpeg.Encoder.VideoToolbox;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.State;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Encoder;

public static class AvailableEncoders
{
    public static Option<IEncoder> ForVideoFormat(
        IHardwareCapabilities hardwareCapabilities,
        FFmpegState ffmpegState,
        FrameState currentState,
        FrameState desiredState,
        Option<WatermarkInputFile> maybeWatermarkInputFile,
        Option<SubtitleInputFile> maybeSubtitleInputFile,
        ILogger logger) =>
        (ffmpegState.EncoderHardwareAccelerationMode, desiredState.VideoFormat) switch
        {
            (HardwareAccelerationMode.Nvenc, VideoFormat.Hevc) when hardwareCapabilities.CanEncode(
                    VideoFormat.Hevc,
                    desiredState.PixelFormat) =>
                new EncoderHevcNvenc(
                    currentState,
                    maybeWatermarkInputFile,
                    maybeSubtitleInputFile),
            (HardwareAccelerationMode.Nvenc, VideoFormat.H264) when hardwareCapabilities.CanEncode(
                    VideoFormat.H264,
                    desiredState.PixelFormat) =>
                new EncoderH264Nvenc(
                    currentState,
                    maybeWatermarkInputFile,
                    maybeSubtitleInputFile),

            (HardwareAccelerationMode.Qsv, VideoFormat.Hevc) when hardwareCapabilities.CanEncode(
                    VideoFormat.Hevc,
                    desiredState.PixelFormat) =>
                new EncoderHevcQsv(
                    currentState,
                    maybeWatermarkInputFile,
                    maybeSubtitleInputFile),
            (HardwareAccelerationMode.Qsv, VideoFormat.H264) when hardwareCapabilities.CanEncode(
                    VideoFormat.H264,
                    desiredState.PixelFormat) =>
                new EncoderH264Qsv(
                    currentState,
                    maybeWatermarkInputFile,
                    maybeSubtitleInputFile),

            (HardwareAccelerationMode.Vaapi, VideoFormat.Hevc) when hardwareCapabilities.CanEncode(
                    VideoFormat.Hevc,
                    desiredState.PixelFormat) =>
                new EncoderHevcVaapi(
                    currentState,
                    maybeWatermarkInputFile,
                    maybeSubtitleInputFile),
            (HardwareAccelerationMode.Vaapi, VideoFormat.H264) when hardwareCapabilities.CanEncode(
                    VideoFormat.H264,
                    desiredState.PixelFormat) =>
                new EncoderH264Vaapi(
                    currentState,
                    maybeWatermarkInputFile,
                    maybeSubtitleInputFile),

            (HardwareAccelerationMode.VideoToolbox, VideoFormat.Hevc) when hardwareCapabilities.CanEncode(
                VideoFormat.Hevc,
                desiredState.PixelFormat) => new EncoderHevcVideoToolbox(),
            (HardwareAccelerationMode.VideoToolbox, VideoFormat.H264) when hardwareCapabilities.CanEncode(
                VideoFormat.H264,
                desiredState.PixelFormat) => new EncoderH264VideoToolbox(),
            
            (HardwareAccelerationMode.Amf, VideoFormat.Hevc) when hardwareCapabilities.CanEncode(
                VideoFormat.Hevc,
                desiredState.PixelFormat) => new EncoderHevcAmf(),
            (HardwareAccelerationMode.Amf, VideoFormat.H264) when hardwareCapabilities.CanEncode(
                VideoFormat.H264,
                desiredState.PixelFormat) => new EncoderH264Amf(),

            (_, VideoFormat.Hevc) => new EncoderLibx265(currentState),
            (_, VideoFormat.H264) => new EncoderLibx264(),
            (_, VideoFormat.Mpeg2Video) => new EncoderMpeg2Video(),

            (_, VideoFormat.Undetermined) => new EncoderImplicitVideo(),
            (_, VideoFormat.Copy) => new EncoderCopyVideo(),

            var (accel, videoFormat) => LogUnknownEncoder(accel, videoFormat, logger)
        };

    private static Option<IEncoder> LogUnknownEncoder(
        HardwareAccelerationMode hardwareAccelerationMode,
        string videoFormat,
        ILogger logger)
    {
        logger.LogWarning(
            "Unable to determine video encoder for {AccelMode} - {VideoFormat}; may have playback issues",
            hardwareAccelerationMode,
            videoFormat);
        return Option<IEncoder>.None;
    }

    public static Option<IEncoder> ForAudioFormat(AudioState desiredState, ILogger logger) =>
        desiredState.AudioFormat.Match(
            audioFormat =>
                audioFormat switch
                {
                    AudioFormat.Aac => (Option<IEncoder>)new EncoderAac(),
                    AudioFormat.Ac3 => new EncoderAc3(),
                    AudioFormat.Copy => new EncoderCopyAudio(),
                    _ => LogUnknownEncoder(audioFormat, logger)
                },
            () => LogUnknownEncoder(string.Empty, logger));

    private static Option<IEncoder> LogUnknownEncoder(
        string audioFormat,
        ILogger logger)
    {
        logger.LogWarning("Unable to determine audio encoder for {AudioFormat}; may have playback issues", audioFormat);
        return Option<IEncoder>.None;
    }
}
