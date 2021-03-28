namespace ErsatzTV.Core.FFmpeg
{
    public record ConcatPlaylist(string Scheme, string Host, string ChannelNumber)
    {
        public override string ToString() =>
            $@"ffconcat version 1.0
file http://localhost:8409/ffmpeg/stream/{ChannelNumber}
file http://localhost:8409/ffmpeg/stream/{ChannelNumber}";
    }
}
