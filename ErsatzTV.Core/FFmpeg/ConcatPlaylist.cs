﻿namespace ErsatzTV.Core.FFmpeg;

public record ConcatPlaylist(string Scheme, string Host, string ChannelNumber, string Mode)
{
    public override string ToString() =>
        $@"ffconcat version 1.0
file http://localhost:{Settings.ListenPort}/ffmpeg/stream/{ChannelNumber}?mode={Mode}
file http://localhost:{Settings.ListenPort}/ffmpeg/stream/{ChannelNumber}?mode={Mode}";
}
