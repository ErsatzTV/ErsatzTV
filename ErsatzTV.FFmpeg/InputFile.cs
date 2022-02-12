namespace ErsatzTV.FFmpeg;

public record InputFile(string Path, IList<MediaStream> Streams);
