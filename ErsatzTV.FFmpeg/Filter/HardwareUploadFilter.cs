﻿namespace ErsatzTV.FFmpeg.Filter;

public class HardwareUploadFilter : BaseFilter
{
    private readonly FFmpegState _ffmpegState;

    public HardwareUploadFilter(FFmpegState ffmpegState) => _ffmpegState = ffmpegState;

    public override string Filter => _ffmpegState.HardwareAccelerationMode switch
    {
        HardwareAccelerationMode.None => string.Empty,
        HardwareAccelerationMode.Nvenc => "hwupload_cuda",
        HardwareAccelerationMode.Qsv => "hwupload=extra_hw_frames=64",
        HardwareAccelerationMode.Vaapi => "format=nv12|vaapi,hwupload",
        _ => "hwupload"
    };

    public override FrameState NextState(FrameState currentState) => _ffmpegState.HardwareAccelerationMode switch
    {
        HardwareAccelerationMode.None => currentState,
        _ => currentState with { FrameDataLocation = FrameDataLocation.Hardware }
    };
}
