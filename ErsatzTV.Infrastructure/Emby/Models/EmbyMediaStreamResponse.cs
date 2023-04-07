﻿namespace ErsatzTV.Infrastructure.Emby.Models;

public class EmbyMediaStreamResponse
{
    public string Type { get; set; }
    public string Codec { get; set; }
    public string Language { get; set; }
    public bool? IsInterlaced { get; set; }
    public int? Height { get; set; }
    public int? Width { get; set; }
    public int Index { get; set; }
    public bool IsDefault { get; set; }
    public bool IsForced { get; set; }
    public string Profile { get; set; }
    public string AspectRatio { get; set; }
    public int? Channels { get; set; }
    public bool? IsAnamorphic { get; set; }
    public bool? IsExternal { get; set; }
    public string DisplayTitle { get; set; }
    public string PixelFormat { get; set; }
    public string ColorRange { get; set; }
    public string ColorSpace { get; set; }
    public string ColorTransfer { get; set; }
    public string ColorPrimaries { get; set; }
    public double? RealFrameRate { get; set; }
}
