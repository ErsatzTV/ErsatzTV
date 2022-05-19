namespace ErsatzTV.FFmpeg;

public record FrameSize(int Width, int Height)
{
    public static FrameSize Unknown = new(-1, -1);
}
