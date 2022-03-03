﻿namespace ErsatzTV.Infrastructure.Jellyfin.Models;

public class JellyfinMediaStreamResponse
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
}