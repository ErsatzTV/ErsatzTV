using ErsatzTV.FFmpeg.Decoder.Cuvid;
using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Decoder;

public static class AvailableDecoders
{
    public static IDecoder ForVideoFormat(FrameState currentState, FrameState desiredState)
    {
        return (currentState.HardwareAccelerationMode, currentState.VideoFormat) switch
        {
            (HardwareAccelerationMode.Nvenc, VideoFormat.Hevc) => new DecoderHevcCuvid(desiredState),
            (HardwareAccelerationMode.Nvenc, VideoFormat.H264) => new DecoderH264Cuvid(desiredState),
            (HardwareAccelerationMode.Nvenc, VideoFormat.Mpeg2Video) => new DecoderMpeg2Cuvid(desiredState),
            (HardwareAccelerationMode.Nvenc, VideoFormat.Vc1) => new DecoderVc1Cuvid(desiredState),
            (_, VideoFormat.Hevc) => new DecoderHevc(),
            (_, VideoFormat.H264) => new DecoderH264(),
            (_, VideoFormat.Mpeg2Video) => new DecoderMpeg2Video(),
            (_, VideoFormat.Vc1) => new DecoderVc1(),
            _ => throw new ArgumentOutOfRangeException(nameof(currentState.VideoFormat), currentState.VideoFormat, null)
        };
    }
}
