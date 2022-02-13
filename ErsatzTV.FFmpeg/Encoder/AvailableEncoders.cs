using ErsatzTV.FFmpeg.Encoder.Nvenc;
using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder;

public static class AvailableEncoders
{
    public static IEncoder ForVideoFormat(FrameState desiredState) =>
        (desiredState.HardwareAccelerationMode, desiredState.VideoFormat )switch
        {
            (HardwareAccelerationMode.Nvenc, VideoFormat.Hevc) => new EncoderHevcNvenc(),
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
