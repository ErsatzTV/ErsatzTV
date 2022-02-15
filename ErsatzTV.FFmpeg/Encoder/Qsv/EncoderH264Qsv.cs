using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Qsv;

public class EncoderH264Qsv : EncoderBase
{
    private readonly FrameState _currentState;

    public EncoderH264Qsv(FrameState currentState)
    {
        _currentState = currentState;
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.H264,
        FrameDataLocation = FrameDataLocation.Hardware
    };

    public override string Name => "h264_qsv";
    public override StreamKind Kind => StreamKind.Video;

    // need to convert to nv12 if we're still in software
    public override string Filter
    {
        get
        {
            if (_currentState.FrameDataLocation == FrameDataLocation.Software)
            {
                // pixel format should already be converted to a supported format by QsvHardwareAccelerationOption
                foreach (IPixelFormat pixelFormat in _currentState.PixelFormat)
                {
                    return $"format={pixelFormat.FFmpegName},hwupload=extra_hw_frames=64";
                }
                
                // default to nv12
                return "format=nv12,hwupload=extra_hw_frames=64";
            }

            return string.Empty;
        }
    }
}
