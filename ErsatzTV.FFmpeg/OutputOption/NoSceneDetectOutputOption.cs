﻿using System.Globalization;

namespace ErsatzTV.FFmpeg.OutputOption;

public class NoSceneDetectOutputOption : OutputOption
{
    private readonly int _value;

    public NoSceneDetectOutputOption(int value) => _value = value;

    public override string[] OutputOptions => new[]
        { "-sc_threshold", _value.ToString(CultureInfo.InvariantCulture) };
}
