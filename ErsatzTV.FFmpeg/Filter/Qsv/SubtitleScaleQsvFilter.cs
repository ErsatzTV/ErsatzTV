﻿using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter.Qsv;

public class SubtitleScaleQsvFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly int _extraHardwareFrames;
    private readonly FrameSize _paddedSize;
    private readonly FrameSize _scaledSize;

    public SubtitleScaleQsvFilter(
        FrameState currentState,
        FrameSize scaledSize,
        FrameSize paddedSize,
        int extraHardwareFrames)
    {
        _currentState = currentState;
        _scaledSize = scaledSize;
        _paddedSize = paddedSize;
        _extraHardwareFrames = extraHardwareFrames;
    }

    public override string Filter
    {
        get
        {
            string scale = string.Empty;
            if (_currentState.ScaledSize != _scaledSize)
            {
                var targetSize = $"w={_paddedSize.Width}:h={_paddedSize.Height}";

                // use software scaling
                scale = $"scale={targetSize}:force_original_aspect_ratio=decrease";
            }

            string initialPixelFormat = _currentState.PixelFormat.Match(pf => pf.FFmpegName, FFmpegFormat.NV12);
            if (!string.IsNullOrWhiteSpace(scale))
            {
                return $"{scale},format={initialPixelFormat},hwupload=extra_hw_frames={_extraHardwareFrames}";
            }

            return $"format={initialPixelFormat},hwupload=extra_hw_frames={_extraHardwareFrames}";
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState;
}
