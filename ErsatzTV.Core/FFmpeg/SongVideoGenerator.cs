using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.FFmpeg
{
    public class SongVideoGenerator : ISongVideoGenerator
    {
        private static readonly Random Random = new();
        private static readonly object RandomLock = new();
        
        private readonly ITempFilePool _tempFilePool;
        private readonly IImageCache _imageCache;
        private readonly IFFmpegProcessService _ffmpegProcessService;

        public SongVideoGenerator(
            ITempFilePool tempFilePool,
            IImageCache imageCache,
            IFFmpegProcessService ffmpegProcessService)
        {
            _tempFilePool = tempFilePool;
            _imageCache = imageCache;
            _ffmpegProcessService = ffmpegProcessService;
        }

        public async Task<Tuple<string, MediaVersion>> GenerateSongVideo(
            Song song,
            Channel channel,
            Option<ChannelWatermark> maybeGlobalWatermark,
            string ffmpegPath)
        {
            Option<string> subtitleFile = None;

            MediaVersion videoVersion = new FallbackMediaVersion
            {
                Id = -1,
                Chapters = new List<MediaChapter>(),
                Width = 192,
                Height = 108,
                SampleAspectRatio = "1:1",
                Streams = new List<MediaStream>
                {
                    new() { MediaStreamKind = MediaStreamKind.Video, Index = 0 }
                }
            };

            string[] backgrounds =
            {
                "background_blank.png",
                "background_e.png",
                "background_t.png",
                "background_v.png"
            };

            // use random ETV color by default
            string artworkPath = Path.Combine(
                FileSystemLayout.ResourcesCacheFolder,
                backgrounds[NextRandom(backgrounds.Length)]);

            var boxBlur = false;
            Option<int> randomColor = None;
            
            const int HORIZONTAL_MARGIN_PERCENT = 3;
            const int VERTICAL_MARGIN_PERCENT = 5;
            const int WATERMARK_WIDTH_PERCENT = 25;
            ChannelWatermarkLocation watermarkLocation = NextRandom(2) == 0
                ? ChannelWatermarkLocation.BottomLeft
                : ChannelWatermarkLocation.BottomRight;
            
            foreach (SongMetadata metadata in song.SongMetadata)
            {
                var fontSize = (int)Math.Round(channel.FFmpegProfile.Resolution.Height / 20.0);
                var largeFontSize = (int)Math.Round(channel.FFmpegProfile.Resolution.Height / 10.0);
                bool detailsStyle = NextRandom(2) == 0;
                
                var sb = new StringBuilder();

                if (detailsStyle)
                {
                    if (!string.IsNullOrWhiteSpace(metadata.Title))
                    {
                        sb.Append($"{{\\fs{largeFontSize}}}{metadata.Title}");
                    }
                    
                    if (!string.IsNullOrWhiteSpace(metadata.Artist))
                    {
                        sb.Append($"\\N{{\\fs{fontSize}}}{metadata.Artist}");
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(metadata.Artist))
                    {
                        sb.Append(metadata.Artist);
                    }

                    if (!string.IsNullOrWhiteSpace(metadata.Title))
                    {
                        sb.Append($"\\N\"{metadata.Title}\"");
                    }

                    if (!string.IsNullOrWhiteSpace(metadata.Album))
                    {
                        sb.Append($"\\N{metadata.Album}");
                    }
                }

                int leftMarginPercent = HORIZONTAL_MARGIN_PERCENT;
                int rightMarginPercent = HORIZONTAL_MARGIN_PERCENT;

                switch (watermarkLocation)
                {
                    case ChannelWatermarkLocation.BottomLeft:
                        leftMarginPercent += WATERMARK_WIDTH_PERCENT + HORIZONTAL_MARGIN_PERCENT;
                        break;
                    case ChannelWatermarkLocation.BottomRight:
                        leftMarginPercent = rightMarginPercent = HORIZONTAL_MARGIN_PERCENT;
                        rightMarginPercent += WATERMARK_WIDTH_PERCENT + HORIZONTAL_MARGIN_PERCENT;
                        break;
                }

                var leftMargin = (int)Math.Round(leftMarginPercent / 100.0 * channel.FFmpegProfile.Resolution.Width);
                var rightMargin = (int)Math.Round(rightMarginPercent / 100.0 * channel.FFmpegProfile.Resolution.Width);
                var verticalMargin = (int)Math.Round(VERTICAL_MARGIN_PERCENT / 100.0 * channel.FFmpegProfile.Resolution.Height);

                subtitleFile = await new SubtitleBuilder(_tempFilePool)
                    .WithResolution(channel.FFmpegProfile.Resolution)
                    .WithFontName("OPTIKabel-Heavy")
                    .WithFontSize(fontSize)
                    .WithPrimaryColor("&HFFFFFF")
                    .WithOutlineColor("&H555555")
                    .WithAlignment(0)
                    .WithMarginRight(rightMargin)
                    .WithMarginLeft(leftMargin)
                    .WithMarginV(verticalMargin)
                    .WithBorderStyle(1)
                    .WithShadow(3)
                    .WithFormattedContent(sb.ToString())
                    .BuildFile();
                
                // use thumbnail (cover art) if present
                foreach (Artwork artwork in Optional(
                    metadata.Artwork.Find(a => a.ArtworkKind == ArtworkKind.Thumbnail)))
                {
                    int backgroundRoll = NextRandom(16);
                    if (backgroundRoll < 8)
                    {
                        randomColor = backgroundRoll;
                    }
                    else
                    {
                        boxBlur = true;
                    }

                    string customPath = _imageCache.GetPathForImage(
                        artwork.Path,
                        ArtworkKind.Thumbnail,
                        Option<int>.None);

                    artworkPath = customPath;

                    // signal that we want to use cover art as watermark
                    videoVersion = new CoverArtMediaVersion
                    {
                        Chapters = new List<MediaChapter>(),
                        // always stretch cover art
                        Width = 192,
                        Height = 108,
                        SampleAspectRatio = "1:1",
                        Streams = new List<MediaStream>
                        {
                            new() { MediaStreamKind = MediaStreamKind.Video, Index = 0 }
                        }
                    };
                }
            }

            string videoPath = artworkPath;

            videoVersion.MediaFiles = new List<MediaFile>
            {
                new() { Path = videoPath }
            };

            Either<BaseError, string> maybeSongImage = await _ffmpegProcessService.GenerateSongImage(
                ffmpegPath,
                subtitleFile,
                channel,
                maybeGlobalWatermark,
                videoVersion,
                videoPath,
                boxBlur,
                randomColor,
                watermarkLocation,
                HORIZONTAL_MARGIN_PERCENT,
                VERTICAL_MARGIN_PERCENT,
                WATERMARK_WIDTH_PERCENT);

            foreach (string si in maybeSongImage.RightToSeq())
            {
                videoPath = si;
                videoVersion = new BackgroundImageMediaVersion
                {
                    Chapters = new List<MediaChapter>(),
                    // song image has been pre-generated with correct size
                    Height = channel.FFmpegProfile.Resolution.Height,
                    Width = channel.FFmpegProfile.Resolution.Width,
                    SampleAspectRatio = "1:1",
                    Streams = new List<MediaStream>
                    {
                        new() { MediaStreamKind = MediaStreamKind.Video, Index = 0 },
                    },
                    MediaFiles = new List<MediaFile>
                    {
                        new() { Path = si }
                    }
                };
            }

            return Tuple(videoPath, videoVersion);
        }
        
        private static int NextRandom(int max)
        {
            lock (RandomLock)
            {
                return Random.Next() % max;
            }
        }
    }
}
