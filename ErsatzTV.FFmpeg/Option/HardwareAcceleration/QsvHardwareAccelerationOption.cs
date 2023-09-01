using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Option.HardwareAcceleration;

public class QsvHardwareAccelerationOption : GlobalOption
{
    private readonly Option<string> _qsvDevice;

    // TODO: read this from ffmpeg output
    private readonly List<string> _supportedFFmpegFormats = new()
    {
        FFmpegFormat.NV12,
        FFmpegFormat.P010LE
    };

    public QsvHardwareAccelerationOption(Option<string> qsvDevice) => _qsvDevice = qsvDevice;

    public override IList<string> GlobalOptions
    {
        get
        {
            string[] initDevices = OperatingSystem.IsWindows()
                ? new[] { "-init_hw_device", "d3d11va=hw:,vendor=0x8086", "-filter_hw_device", "hw" }
                : new[] { "-init_hw_device", "qsv=hw", "-filter_hw_device", "hw" };

            var result = new List<string>
            {
                "-hwaccel", "qsv",
                "-hwaccel_output_format", "qsv"
            };

            if (OperatingSystem.IsLinux())
            {
                foreach (string qsvDevice in _qsvDevice)
                {
                    if (!string.IsNullOrWhiteSpace(qsvDevice))
                    {
                        result.AddRange(new[] { "-qsv_device", qsvDevice });
                    }
                }
            }

            result.AddRange(initDevices);

            return result;
        }
    }

    // qsv encoders want nv12
    public override FrameState NextState(FrameState currentState)
    {
        FrameState result = currentState;

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
