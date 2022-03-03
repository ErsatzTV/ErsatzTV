﻿namespace ErsatzTV.Core.Domain;

public class MediaStream
{
    public int Id { get; set; }
    public int Index { get; set; }
    public string Codec { get; set; }
    public string Profile { get; set; }
    public MediaStreamKind MediaStreamKind { get; set; }
    public string Language { get; set; }
    public int Channels { get; set; }
    public string Title { get; set; }
    public bool Default { get; set; }
    public bool Forced { get; set; }
    public bool AttachedPic { get; set; }
    public string PixelFormat { get; set; }
    public int BitsPerRawSample { get; set; }
    public int MediaVersionId { get; set; }
    public MediaVersion MediaVersion { get; set; }
}