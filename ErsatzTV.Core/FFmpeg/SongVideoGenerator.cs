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

            bool boxBlur = false;
            Option<int> randomColor = None;

            // use thumbnail (cover art) if present
            foreach (SongMetadata metadata in song.SongMetadata)
            {
                string fileName = _tempFilePool.GetNextTempFile(TempFileCategory.Subtitle);
                subtitleFile = fileName;

                var sb = new StringBuilder();
                sb.AppendLine("1");
                sb.AppendLine("00:00:00,000 --> 99:99:99,999");

                if (!string.IsNullOrWhiteSpace(metadata.Artist))
                {
                    sb.AppendLine(metadata.Artist);
                }

                if (!string.IsNullOrWhiteSpace(metadata.Title))
                {
                    sb.AppendLine($"\"{metadata.Title}\"");
                }

                if (!string.IsNullOrWhiteSpace(metadata.Album))
                {
                    sb.AppendLine(metadata.Album);
                }

                await File.WriteAllTextAsync(fileName, sb.ToString());

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
                randomColor);

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
