using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Option.HardwareAcceleration;

public class QsvHardwareAccelerationOption : GlobalOption
{
    // TODO: read this from ffmpeg output
    private readonly List<string> _supportedFFmpegFormats = new()
    {
        FFmpegFormat.NV12,
        FFmpegFormat.P010LE
    };

    public override IList<string> GlobalOptions => new List<string>
    {
        "-hwaccel", "qsv",
        "-init_hw_device", "qsv=qsv:MFX_IMPL_hw_any"
    };

    // qsv encoders want nv12
    public override FrameState NextState(FrameState currentState)
    {
        FrameState result = currentState with { HardwareAccelerationMode = HardwareAccelerationMode.Qsv }; 
        
        foreach (IPixelFormat pixelFormat in currentState.PixelFormat)
        {
            if (_supportedFFmpegFormats.Contains(pixelFormat.FFmpegName))
            {
                return result;
            }

            return currentState with { PixelFormat = new PixelFormatNv12(pixelFormat.Name) };
        }

        return currentState with
        {
            HardwareAccelerationMode = HardwareAccelerationMode.Qsv,
            PixelFormat = new PixelFormatNv12(new PixelFormatUnknown().Name)
        };
    }
}
