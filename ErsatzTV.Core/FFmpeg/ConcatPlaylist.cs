namespace ErsatzTV.Core.FFmpeg
{
    public record ConcatPlaylist(string Scheme, string Host, string ChannelNumber)
    {
        public override string ToString() =>
            $@"ffconcat version 1.0
file {Scheme}://{Host}/ffmpeg/stream/{ChannelNumber}
file {Scheme}://{Host}/ffmpeg/stream/{ChannelNumber}";
    }
}
