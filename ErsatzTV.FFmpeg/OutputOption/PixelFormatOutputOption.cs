﻿using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.OutputOption;

public class PixelFormatOutputOption : OutputOption
{
    private readonly IPixelFormat _pixelFormat;

    public PixelFormatOutputOption(IPixelFormat pixelFormat) => _pixelFormat = pixelFormat;

    public override string[] OutputOptions => ["-pix_fmt", _pixelFormat.Name];

    public override FrameState NextState(FrameState currentState) =>
        currentState with { PixelFormat = Some(_pixelFormat) };
}
