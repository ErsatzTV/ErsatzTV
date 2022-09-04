﻿namespace ErsatzTV.FFmpeg.Filter;

public class WatermarkHardwareUploadFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly FFmpegState _ffmpegState;

    public WatermarkHardwareUploadFilter(FrameState currentState, FFmpegState ffmpegState)
    {
        _currentState = currentState;
        _ffmpegState = ffmpegState;
    }

    public override string Filter => _ffmpegState.EncoderHardwareAccelerationMode switch
    {
        HardwareAccelerationMode.None => string.Empty,
        HardwareAccelerationMode.Nvenc => "hwupload_cuda",
        HardwareAccelerationMode.Qsv => $"hwupload=extra_hw_frames={_ffmpegState.QsvExtraHardwareFrames}",

        // leave vaapi in software since we don't (yet) use overlay_vaapi
        HardwareAccelerationMode.Vaapi when _currentState.FrameDataLocation == FrameDataLocation.Software =>
            string.Empty,

        // leave videotoolbox in software since we use a software overlay filter
        HardwareAccelerationMode.VideoToolbox => string.Empty,

        // leave amf in software since we use a software overlay filter
        HardwareAccelerationMode.Amf => string.Empty,

        _ => "hwupload"
    };

    public override FrameState NextState(FrameState currentState) => currentState;
}
