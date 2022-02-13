using ErsatzTV.FFmpeg.Encoder.Nvenc;
using ErsatzTV.FFmpeg.Encoder.Qsv;
using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder;

public static class AvailableEncoders
{
    public static IEncoder ForVideoFormat(FrameState desiredState) =>
        (desiredState.HardwareAccelerationMode, desiredState.VideoFormat )switch
        {
            (HardwareAccelerationMode.Nvenc, VideoFormat.Hevc) => new EncoderHevcNvenc(),
            (HardwareAccelerationMode.Nvenc, VideoFormat.H264) => new EncoderH264Nvenc(),

            (HardwareAccelerationMode.Qsv, VideoFormat.Hevc) => new EncoderHevcQsv(),
            (HardwareAccelerationMode.Qsv, VideoFormat.H264) => new EncoderH264Qsv(),

            (_, VideoFormat.Hevc) => new EncoderLibx265(),
            (_, VideoFormat.H264) => new EncoderLibx264(),
            (_, VideoFormat.Mpeg2Video )=> new EncoderMpeg2Video(),

            _ => throw new ArgumentOutOfRangeException(nameof(desiredState.VideoFormat), desiredState.VideoFormat, null)
        };

    public static IEncoder ForAudioFormat(FrameState desiredState) =>
        desiredState.AudioFormat switch
        {
            AudioFormat.Aac => new EncoderAac(),
            AudioFormat.Ac3 => new EncoderAc3(),
            _ => throw new ArgumentOutOfRangeException(nameof(desiredState.AudioFormat), desiredState.AudioFormat, null)
        };
}
