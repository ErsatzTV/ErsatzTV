using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Bugsnag;
using CliWrap;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using ErsatzTV.FFmpeg.State;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Serilog;

namespace ErsatzTV.Core.Tests.FFmpeg;

[TestFixture]
[Explicit]
public class TranscodingTests
{
    private static readonly ILoggerFactory LoggerFactory;

    static TranscodingTests()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        LoggerFactory = new LoggerFactory().AddSerilog(Log.Logger);
    }

    [Test]
    [Explicit]
    public void DeleteTestVideos()
    {
        foreach (string file in Directory.GetFiles(TestContext.CurrentContext.TestDirectory, "*.mkv"))
        {
            File.Delete(file);
        }

        Assert.Pass();
    }

    public record InputFormat(string Encoder, string PixelFormat);

    public enum Padding
    {
        NoPadding,
        WithPadding
    }

    public enum Watermark
    {
        None,
        PermanentOpaqueScaled,
        PermanentOpaqueActualSize,
        PermanentTransparentScaled,
        PermanentTransparentActualSize,
        IntermittentOpaque,
        IntermittentTransparent

        // TODO: animated vs static
    }

    public enum Subtitle
    {
        None,
        Picture,
        Text
    }

    private class TestData
    {
        public static Watermark[] Watermarks =
        {
            Watermark.None,
            Watermark.PermanentOpaqueScaled,
            Watermark.PermanentOpaqueActualSize,
            Watermark.PermanentTransparentScaled,
            Watermark.PermanentTransparentActualSize
        };

        public static Subtitle[] Subtitles =
        {
            Subtitle.None,
            Subtitle.Picture,
            Subtitle.Text
        };

        public static Padding[] Paddings =
        {
            Padding.NoPadding,
            Padding.WithPadding
        };

        public static VideoScanKind[] VideoScanKinds =
        {
            VideoScanKind.Progressive,
            VideoScanKind.Interlaced
        };

        public static InputFormat[] InputFormats =
        {
            new("libx264", "yuv420p"),
            new("libx264", "yuvj420p"),
            new("libx264", "yuv420p10le"),
            // new("libx264", "yuv444p10le"),

            new("mpeg1video", "yuv420p"),

            new("mpeg2video", "yuv420p"),

            new("libx265", "yuv420p"),
            new("libx265", "yuv420p10le"),

            new("mpeg4", "yuv420p"),

            new("libvpx-vp9", "yuv420p"),

            // new("libaom-av1", "yuv420p")
            // av1    yuv420p10le    51

            new("msmpeg4v2", "yuv420p"),
            new("msmpeg4v3", "yuv420p")

            // wmv3    yuv420p    1
        };

        public static Resolution[] Resolutions =
        {
            new() { Width = 1920, Height = 1080 },
            new() { Width = 1280, Height = 720 }
        };

        public static HardwareAccelerationKind[] NoAcceleration =
        {
            HardwareAccelerationKind.None
        };

        public static FFmpegProfileVideoFormat[] VideoFormats =
        {
            FFmpegProfileVideoFormat.H264,
            FFmpegProfileVideoFormat.Hevc
        };

        public static HardwareAccelerationKind[] NvidiaAcceleration =
        {
            HardwareAccelerationKind.Nvenc
        };

        public static HardwareAccelerationKind[] VaapiAcceleration =
        {
            HardwareAccelerationKind.Vaapi
        };

        public static HardwareAccelerationKind[] VideoToolboxAcceleration =
        {
            HardwareAccelerationKind.VideoToolbox
        };

        public static HardwareAccelerationKind[] QsvAcceleration =
        {
            HardwareAccelerationKind.Qsv
        };
    }

    [Test]
    [Combinatorial]
    public async Task Transcode(
            [ValueSource(typeof(TestData), nameof(TestData.InputFormats))]
            InputFormat inputFormat,
            [ValueSource(typeof(TestData), nameof(TestData.Resolutions))]
            Resolution profileResolution,
            [ValueSource(typeof(TestData), nameof(TestData.Paddings))]
            Padding padding,
            [ValueSource(typeof(TestData), nameof(TestData.VideoScanKinds))]
            VideoScanKind videoScanKind,
            [ValueSource(typeof(TestData), nameof(TestData.Watermarks))]
            Watermark watermark,
            [ValueSource(typeof(TestData), nameof(TestData.Subtitles))]
            Subtitle subtitle,
            [ValueSource(typeof(TestData), nameof(TestData.VideoFormats))]
            FFmpegProfileVideoFormat profileVideoFormat,
            // [ValueSource(typeof(TestData), nameof(TestData.NoAcceleration))] HardwareAccelerationKind profileAcceleration)
            [ValueSource(typeof(TestData), nameof(TestData.NvidiaAcceleration))]
            HardwareAccelerationKind profileAcceleration)
        // [ValueSource(typeof(TestData), nameof(TestData.VaapiAcceleration))] HardwareAccelerationKind profileAcceleration)
        // [ValueSource(typeof(TestData), nameof(TestData.QsvAcceleration))] HardwareAccelerationKind profileAcceleration)
        // [ValueSource(typeof(TestData), nameof(TestData.VideoToolboxAcceleration))] HardwareAccelerationKind profileAcceleration)
    {
        if (inputFormat.Encoder is "mpeg1video" or "msmpeg4v2" or "msmpeg4v3")
        {
            if (videoScanKind == VideoScanKind.Interlaced)
            {
                Assert.Inconclusive($"{inputFormat.Encoder} does not support interlaced content");
                return;
            }
        }

        string name = GetStringSha256Hash(
            $"{inputFormat.Encoder}_{inputFormat.PixelFormat}_{videoScanKind}_{padding}_{watermark}_{subtitle}_{profileResolution}_{profileVideoFormat}_{profileAcceleration}");

        string file = Path.Combine(TestContext.CurrentContext.TestDirectory, $"{name}.mkv");
        if (!File.Exists(file))
        {
            string resolution = padding == Padding.WithPadding ? "1920x1060" : "1920x1080";

            string videoFilter = videoScanKind == VideoScanKind.Interlaced
                ? "-vf tinterlace=interleave_top,fieldorder=tff"
                : string.Empty;
            string flags = videoScanKind == VideoScanKind.Interlaced ? "-flags +ildct+ilme" : string.Empty;

            string args =
                $"-y -f lavfi -i anoisesrc=color=brown -f lavfi -i testsrc=duration=1:size={resolution}:rate=30 {videoFilter} -c:a aac -c:v {inputFormat.Encoder} -shortest -pix_fmt {inputFormat.PixelFormat} -strict -2 {flags} {file}";
            var p1 = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ExecutableName("ffmpeg"),
                    Arguments = args
                }
            };

            p1.Start();
            await p1.WaitForExitAsync();
            // ReSharper disable once MethodHasAsyncOverload
            p1.WaitForExit();
            p1.ExitCode.Should().Be(0);

            switch (subtitle)
            {
                case Subtitle.Text or Subtitle.Picture:
                    string sourceFile = Path.GetTempFileName() + ".mkv";
                    File.Move(file, sourceFile, true);

                    string tempFileName = Path.GetTempFileName() + ".mkv";
                    string subPath = Path.Combine(
                        TestContext.CurrentContext.TestDirectory,
                        "Resources",
                        subtitle == Subtitle.Picture ? "test.sup" : "test.srt");
                    var p2 = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = ExecutableName("mkvmerge"),
                            Arguments = $"-o {tempFileName} {sourceFile} {subPath}"
                        }
                    };

                    p2.Start();
                    await p2.WaitForExitAsync();
                    // ReSharper disable once MethodHasAsyncOverload
                    p2.WaitForExit();
                    if (p2.ExitCode != 0)
                    {
                        if (File.Exists(sourceFile))
                        {
                            File.Delete(sourceFile);
                        }

                        if (File.Exists(file))
                        {
                            File.Delete(file);
                        }
                    }

                    p2.ExitCode.Should().Be(0);

                    File.Move(tempFileName, file, true);
                    break;
            }
        }

        var imageCache = new Mock<IImageCache>();

        // always return the static watermark resource
        imageCache.Setup(
                ic => ic.GetPathForImage(
                    It.IsAny<string>(),
                    It.Is<ArtworkKind>(x => x == ArtworkKind.Watermark),
                    It.IsAny<Option<int>>()))
            .Returns(Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources", "ErsatzTV.png"));

        var oldService = new FFmpegProcessService(
            new FFmpegPlaybackSettingsCalculator(),
            new FakeStreamSelector(),
            imageCache.Object,
            new Mock<ITempFilePool>().Object,
            new Mock<IClient>().Object,
            new MemoryCache(new MemoryCacheOptions()),
            LoggerFactory.CreateLogger<FFmpegProcessService>());

        var service = new FFmpegLibraryProcessService(
            oldService,
            new FFmpegPlaybackSettingsCalculator(),
            new FakeStreamSelector(),
            new Mock<ITempFilePool>().Object,
            LoggerFactory.CreateLogger<FFmpegLibraryProcessService>());

        var v = new MediaVersion
        {
            MediaFiles = new List<MediaFile>
            {
                new() { Path = file }
            },
            Streams = new List<MediaStream>()
        };

        var metadataRepository = new Mock<IMetadataRepository>();
        metadataRepository
            .Setup(r => r.UpdateLocalStatistics(It.IsAny<MediaItem>(), It.IsAny<MediaVersion>(), It.IsAny<bool>()))
            .Callback<MediaItem, MediaVersion, bool>(
                (_, version, _) =>
                {
                    version.MediaFiles = v.MediaFiles;
                    v = version;
                });

        var localStatisticsProvider = new LocalStatisticsProvider(
            metadataRepository.Object,
            new LocalFileSystem(new Mock<IClient>().Object, LoggerFactory.CreateLogger<LocalFileSystem>()),
            new Mock<IClient>().Object,
            LoggerFactory.CreateLogger<LocalStatisticsProvider>());

        await localStatisticsProvider.RefreshStatistics(
            ExecutableName("ffmpeg"),
            ExecutableName("ffprobe"),
            new Movie
            {
                MediaVersions = new List<MediaVersion>
                {
                    new()
                    {
                        MediaFiles = new List<MediaFile>
                        {
                            new() { Path = file }
                        }
                    }
                }
            });

        var subtitleStreams = v.Streams
            .Filter(s => s.MediaStreamKind == MediaStreamKind.Subtitle)
            .ToList();

        var subtitles = new List<Domain.Subtitle>();

        foreach (MediaStream stream in subtitleStreams)
        {
            var s = new Domain.Subtitle
            {
                Codec = stream.Codec,
                Default = stream.Default,
                Forced = stream.Forced,
                Language = stream.Language,
                StreamIndex = stream.Index,
                SubtitleKind = SubtitleKind.Embedded,
                DateAdded = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
                Path = "test.srt",
                IsExtracted = true
            };

            subtitles.Add(s);
        }

        DateTimeOffset now = DateTimeOffset.Now;

        Option<ChannelWatermark> channelWatermark = Option<ChannelWatermark>.None;
        switch (watermark)
        {
            case Watermark.None:
                break;
            case Watermark.IntermittentOpaque:
                channelWatermark = new ChannelWatermark
                {
                    ImageSource = ChannelWatermarkImageSource.Custom,
                    Mode = ChannelWatermarkMode.Intermittent,
                    // TODO: how do we make sure this actually appears
                    FrequencyMinutes = 1,
                    DurationSeconds = 2,
                    Opacity = 100
                };
                break;
            case Watermark.IntermittentTransparent:
                channelWatermark = new ChannelWatermark
                {
                    ImageSource = ChannelWatermarkImageSource.Custom,
                    Mode = ChannelWatermarkMode.Intermittent,
                    // TODO: how do we make sure this actually appears
                    FrequencyMinutes = 1,
                    DurationSeconds = 2,
                    Opacity = 80
                };
                break;
            case Watermark.PermanentOpaqueScaled:
                channelWatermark = new ChannelWatermark
                {
                    ImageSource = ChannelWatermarkImageSource.Custom,
                    Mode = ChannelWatermarkMode.Permanent,
                    Opacity = 100,
                    Size = WatermarkSize.Scaled
                };
                break;
            case Watermark.PermanentOpaqueActualSize:
                channelWatermark = new ChannelWatermark
                {
                    ImageSource = ChannelWatermarkImageSource.Custom,
                    Mode = ChannelWatermarkMode.Permanent,
                    Opacity = 100,
                    Size = WatermarkSize.ActualSize
                };
                break;
            case Watermark.PermanentTransparentScaled:
                channelWatermark = new ChannelWatermark
                {
                    ImageSource = ChannelWatermarkImageSource.Custom,
                    Mode = ChannelWatermarkMode.Permanent,
                    Opacity = 80,
                    Size = WatermarkSize.Scaled
                };
                break;
            case Watermark.PermanentTransparentActualSize:
                channelWatermark = new ChannelWatermark
                {
                    ImageSource = ChannelWatermarkImageSource.Custom,
                    Mode = ChannelWatermarkMode.Permanent,
                    Opacity = 80,
                    Size = WatermarkSize.ActualSize
                };
                break;
        }

        ChannelSubtitleMode subtitleMode = subtitle switch
        {
            Subtitle.Picture or Subtitle.Text => ChannelSubtitleMode.Any,
            _ => ChannelSubtitleMode.None
        };

        string srtFile = Path.Combine(FileSystemLayout.SubtitleCacheFolder, "test.srt");
        if (subtitle == Subtitle.Text && !File.Exists(srtFile))
        {
            string sourceFile = Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources", "test.srt");
            Directory.CreateDirectory(FileSystemLayout.SubtitleCacheFolder);
            File.Copy(sourceFile, srtFile, true);
        }

        Command process = await service.ForPlayoutItem(
            ExecutableName("ffmpeg"),
            ExecutableName("ffprobe"),
            false,
            new Channel(Guid.NewGuid())
            {
                Number = "1",
                FFmpegProfile = FFmpegProfile.New("test", profileResolution) with
                {
                    HardwareAcceleration = profileAcceleration,
                    VideoFormat = profileVideoFormat,
                    AudioFormat = FFmpegProfileAudioFormat.Aac,
                    DeinterlaceVideo = true
                },
                StreamingMode = StreamingMode.TransportStream,
                SubtitleMode = subtitleMode
            },
            v,
            v,
            file,
            file,
            subtitles,
            string.Empty,
            string.Empty,
            subtitleMode,
            now,
            now + TimeSpan.FromSeconds(5),
            now,
            Option<ChannelWatermark>.None,
            channelWatermark,
            VaapiDriver.Default,
            "/dev/dri/renderD128",
            false,
            FillerKind.None,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(5),
            0,
            None);

        // Console.WriteLine($"ffmpeg arguments {string.Join(" ", process.StartInfo.ArgumentList)}");

        string[] unsupportedMessages =
        {
            "No support for codec",
            "No usable",
            "Provided device doesn't support",
            "Current pixel format is unsupported"
        };

        var sb = new StringBuilder();
        CommandResult result;
        var timeoutSignal = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            result = await process
                .WithStandardOutputPipe(PipeTarget.ToStream(Stream.Null))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(sb))
                .ExecuteAsync(timeoutSignal.Token);
        }
        catch (OperationCanceledException)
        {
            Assert.Fail($"Transcode failure (timeout): ffmpeg {process.Arguments}");
            return;
        }

        var error = sb.ToString();
        bool isUnsupported = unsupportedMessages.Any(error.Contains);

        if (profileAcceleration != HardwareAccelerationKind.None && isUnsupported)
        {
            result.ExitCode.Should().Be(1, $"Error message with successful exit code? {process.Arguments}");
            Assert.Warn($"Unsupported on this hardware: ffmpeg {process.Arguments}");
        }
        else if (error.Contains("Impossible to convert between"))
        {
            Assert.Fail($"Transcode failure: ffmpeg {process.Arguments}");
        }
        else
        {
            result.ExitCode.Should().Be(0, error + Environment.NewLine + process.Arguments);
            if (result.ExitCode == 0)
            {
                Console.WriteLine(process.Arguments);
            }
        }
    }

    private static string GetStringSha256Hash(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        using var sha = SHA256.Create();
        byte[] textData = Encoding.UTF8.GetBytes(text);
        byte[] hash = sha.ComputeHash(textData);
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }

    private class FakeStreamSelector : IFFmpegStreamSelector
    {
        public Task<MediaStream> SelectVideoStream(MediaVersion version) =>
            version.Streams.First(s => s.MediaStreamKind == MediaStreamKind.Video).AsTask();

        public Task<Option<MediaStream>> SelectAudioStream(
            MediaVersion version,
            StreamingMode streamingMode,
            string channelNumber,
            string preferredAudioLanguage) =>
            Optional(version.Streams.First(s => s.MediaStreamKind == MediaStreamKind.Audio)).AsTask();

        public Task<Option<Domain.Subtitle>> SelectSubtitleStream(
            MediaVersion version,
            List<Domain.Subtitle> subtitles,
            StreamingMode streamingMode,
            string channelNumber,
            string preferredSubtitleLanguage,
            ChannelSubtitleMode subtitleMode) =>
            subtitles.HeadOrNone().AsTask();
    }

    private static string ExecutableName(string baseName) =>
        OperatingSystem.IsWindows() ? $"{baseName}.exe" : baseName;
}
