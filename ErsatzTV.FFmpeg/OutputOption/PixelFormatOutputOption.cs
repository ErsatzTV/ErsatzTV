using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.OutputOption;

public class PixelFormatOutputOption(
    IPixelFormat pixelFormat,
    HardwareAccelerationMode encoderMode = HardwareAccelerationMode.None)
    : OutputOption
{

    public override string[] OutputOptions =>
    [
        "-pix_fmt", encoderMode is HardwareAccelerationMode.Nvenc ? pixelFormat.FFmpegName : pixelFormat.Name
    ];

    public override FrameState NextState(FrameState currentState) =>
        currentState with { PixelFormat = Some(pixelFormat) };
}
