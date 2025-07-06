using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.Core.Domain;

public class BackgroundImageMediaVersion : MediaVersion
{
    public bool IsSongWithProgress { get; private set; }

    public static BackgroundImageMediaVersion ForPath(
        string path,
        IDisplaySize resolution,
        bool isSongWithProgress = false) =>
        new()
        {
            Chapters = [],
            // image has been pre-generated with correct size
            Height = resolution.Height,
            Width = resolution.Width,
            SampleAspectRatio = "1:1",
            Streams =
            [
                new MediaStream
                {
                    MediaStreamKind = MediaStreamKind.Video,
                    Index = 0,
                    Codec = VideoFormat.GeneratedImage,
                    PixelFormat = new PixelFormatUnknown().Name // the resulting pixel format is unknown
                }
            ],
            MediaFiles = [new MediaFile { Path = path }],
            IsSongWithProgress = isSongWithProgress
        };
}
