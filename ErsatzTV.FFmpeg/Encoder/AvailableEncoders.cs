using ErsatzTV.FFmpeg.Encoder.Nvenc;
using ErsatzTV.FFmpeg.Encoder.Qsv;
using ErsatzTV.FFmpeg.Encoder.Vaapi;
using ErsatzTV.FFmpeg.Encoder.VideoToolbox;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.State;
using Microsoft.Extensions.Logging;
using LanguageExt;

namespace ErsatzTV.FFmpeg.Encoder;

public static class AvailableEncoders
{
    public static Option<IEncoder> ForVideoFormat(
        FFmpegState ffmpegState,
        FrameState currentState,
        FrameState desiredState,
        Option<WatermarkInputFile> maybeWatermarkInputFile,
        ILogger logger) =>
        (ffmpegState.HardwareAccelerationMode, desiredState.VideoFormat) switch
        {
            (HardwareAccelerationMode.Nvenc, VideoFormat.Hevc) => new EncoderHevcNvenc(),
            (HardwareAccelerationMode.Nvenc, VideoFormat.H264) => new EncoderH264Nvenc(),

            (HardwareAccelerationMode.Qsv, VideoFormat.Hevc) => new EncoderHevcQsv(
                currentState,
                maybeWatermarkInputFile),
            (HardwareAccelerationMode.Qsv, VideoFormat.H264) => new EncoderH264Qsv(currentState),

            (HardwareAccelerationMode.Vaapi, VideoFormat.Hevc) => new EncoderHevcVaapi(currentState),
            (HardwareAccelerationMode.Vaapi, VideoFormat.H264) => new EncoderH264Vaapi(currentState),

            (HardwareAccelerationMode.VideoToolbox, VideoFormat.Hevc) => new EncoderHevcVideoToolbox(),
            (HardwareAccelerationMode.VideoToolbox, VideoFormat.H264) => new EncoderH264VideoToolbox(),

            (_, VideoFormat.Hevc) => new EncoderLibx265(),
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

    public static Option<IEncoder> ForAudioFormat(AudioState desiredState, ILogger logger)
    {
        return desiredState.AudioFormat.Match(
            audioFormat =>
                audioFormat switch
                {
                    AudioFormat.Aac => (Option<IEncoder>)new EncoderAac(),
                    AudioFormat.Ac3 => new EncoderAc3(),
                    AudioFormat.Copy => new EncoderCopyAudio(),
                    _ => LogUnknownEncoder(audioFormat, logger)
                },
            () => LogUnknownEncoder(string.Empty, logger));
    }
    
    private static Option<IEncoder> LogUnknownEncoder(
        string audioFormat,
        ILogger logger)
    {
        logger.LogWarning("Unable to determine audio encoder for {AudioFormat}; may have playback issues", audioFormat);
        return Option<IEncoder>.None;
    }
}
