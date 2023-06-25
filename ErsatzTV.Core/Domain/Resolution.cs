﻿using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Core.Domain;

public class Resolution : IDisplaySize
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Height { get; set; }
    public int Width { get; set; }
    public bool IsCustom { get; set; }

    public override string ToString() => $"{Width}x{Height}";
}
