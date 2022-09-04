﻿using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter.Qsv;

public class DeinterlaceQsvFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly int _extraHardwareFrames;

    public DeinterlaceQsvFilter(FrameState currentState, int extraHardwareFrames)
    {
        _currentState = currentState;
        _extraHardwareFrames = extraHardwareFrames;
    }

    // deinterlace_qsv seems to only support nv12, not p010le
    public override string Filter => _currentState.FrameDataLocation == FrameDataLocation.Software
        ? $"format=nv12,hwupload=extra_hw_frames={_extraHardwareFrames},deinterlace_qsv"
        : "deinterlace_qsv";

    public override FrameState NextState(FrameState currentState)
    {
        FrameState result = currentState with
        {
            Deinterlaced = true,
            FrameDataLocation = FrameDataLocation.Hardware
        };

        // deinterlace_qsv seems to only support nv12, not p010le
        foreach (IPixelFormat pixelFormat in currentState.PixelFormat)
        {
            if (pixelFormat.FFmpegName != FFmpegFormat.NV12)
            {
                result = result with
                {
                    PixelFormat = new PixelFormatNv12(pixelFormat.Name)
                };
            }
        }

        return result;
    }
}
