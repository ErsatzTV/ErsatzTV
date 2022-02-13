using ErsatzTV.FFmpeg.Decoder.Cuvid;
using ErsatzTV.FFmpeg.Decoder.Qsv;
using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Decoder;

public static class AvailableDecoders
{
    public static IDecoder ForVideoFormat(FrameState currentState, FrameState desiredState)
    {
        return (currentState.HardwareAccelerationMode, currentState.VideoFormat, currentState.PixelFormat.Name) switch
        {
            (HardwareAccelerationMode.Nvenc, VideoFormat.Hevc, _) => new DecoderHevcCuvid(desiredState),
            (HardwareAccelerationMode.Nvenc, VideoFormat.H264, _) => new DecoderH264Cuvid(desiredState),
            (HardwareAccelerationMode.Nvenc, VideoFormat.Mpeg2Video, _) => new DecoderMpeg2Cuvid(desiredState),
            (HardwareAccelerationMode.Nvenc, VideoFormat.Vc1, _) => new DecoderVc1Cuvid(desiredState),
            (HardwareAccelerationMode.Nvenc, VideoFormat.Mpeg4, _) => new DecoderMpeg4Cuvid(desiredState),
            
            // hevc_qsv decoder sometimes causes green lines with 10-bit content
            (HardwareAccelerationMode.Qsv, VideoFormat.Hevc, PixelFormat.YUV420P10LE) => new DecoderHevc(),
            
            (HardwareAccelerationMode.Qsv, VideoFormat.Hevc, _) => new DecoderHevcQsv(),
            (HardwareAccelerationMode.Qsv, VideoFormat.H264, _) => new DecoderH264Qsv(),
            (HardwareAccelerationMode.Qsv, VideoFormat.Mpeg2Video, _) => new DecoderMpeg2Qsv(),
            (HardwareAccelerationMode.Qsv, VideoFormat.Vc1, _) => new DecoderVc1Qsv(),
            
            (_, VideoFormat.Hevc, _) => new DecoderHevc(),
            (_, VideoFormat.H264, _) => new DecoderH264(),
            (_, VideoFormat.Mpeg2Video, _) => new DecoderMpeg2Video(),
            (_, VideoFormat.Vc1, _) => new DecoderVc1(),
            (_, VideoFormat.Mpeg4, _) => new DecoderMpeg4(),
            
            _ => throw new ArgumentOutOfRangeException(nameof(currentState.VideoFormat), currentState.VideoFormat, null)
        };
    }
}
