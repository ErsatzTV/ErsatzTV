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

    public override IList<string> GlobalOptions
    {
        get
        {
            string[] initDevices = OperatingSystem.IsWindows()
                ? new[] { "-init_hw_device", "qsv=hw:hw,child_device_type=dxva2" }
                : new[] { "-init_hw_device", "qsv=hw" };

            var result = new List<string>
            {
                "-hwaccel", "qsv",
                "-filter_hw_device", "hw",
                "-hwaccel_output_format", "qsv"
            };

            result.AddRange(initDevices);

            return result;
        }
    }

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

            return result with { PixelFormat = new PixelFormatNv12(pixelFormat.Name) };
        }

        return result with { PixelFormat = new PixelFormatNv12(new PixelFormatUnknown().Name) };
    }
}
