using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.Core.Domain;

public class BackgroundImageMediaVersion : MediaVersion
{
    public static BackgroundImageMediaVersion ForPath(string path, IDisplaySize resolution) =>
        new()
        {
            Chapters = new List<MediaChapter>(),
            // image has been pre-generated with correct size
            Height = resolution.Height,
            Width = resolution.Width,
            SampleAspectRatio = "1:1",
            Streams = new List<MediaStream>
            {
                new()
                {
                    MediaStreamKind = MediaStreamKind.Video,
                    Index = 0,
                    Codec = VideoFormat.GeneratedImage,
                    PixelFormat = new PixelFormatUnknown().Name // the resulting pixel format is unknown
                }
            },
            MediaFiles = new List<MediaFile>
            {
                new() { Path = path }
            }
        };
}
