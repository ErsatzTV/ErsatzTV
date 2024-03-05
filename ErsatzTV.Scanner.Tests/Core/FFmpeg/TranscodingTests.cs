﻿using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Bugsnag;
using CliWrap;
using CliWrap.Buffered;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using ErsatzTV.FFmpeg;
using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.FFmpeg.Filter;
using ErsatzTV.FFmpeg.Filter.Cuda;
using ErsatzTV.FFmpeg.Filter.Qsv;
using ErsatzTV.FFmpeg.Filter.Vaapi;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.Pipeline;
using ErsatzTV.FFmpeg.State;
using ErsatzTV.Infrastructure.Images;
using ErsatzTV.Infrastructure.Metadata;
using ErsatzTV.Infrastructure.Runtime;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Serilog;
using MediaStream = ErsatzTV.Core.Domain.MediaStream;

namespace ErsatzTV.Scanner.Tests.Core.FFmpeg;

[TestFixture]
[Explicit]
public class TranscodingTests
{
    private static readonly ILoggerFactory LoggerFactory;
    private static readonly MemoryCache MemoryCache;

    static TranscodingTests()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        LoggerFactory = new LoggerFactory().AddSerilog(Log.Logger);

        MemoryCache = new MemoryCache(new MemoryCacheOptions());

        if (!Directory.Exists(FileSystemLayout.TempFilePoolFolder))
        {
            Directory.CreateDirectory(FileSystemLayout.TempFilePoolFolder);
        }
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

    public record InputFormat(
        string Encoder,
        string PixelFormat,
        string ColorRange = "tv",
        string ColorSpace = "bt709",
        string ColorTransfer = "bt709",
        string ColorPrimaries = "bt709");

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
        [
            Watermark.None
            //Watermark.PermanentOpaqueScaled,
            // Watermark.PermanentOpaqueActualSize,
            //Watermark.PermanentTransparentScaled
            // Watermark.PermanentTransparentActualSize
        ];

        public static Subtitle[] Subtitles =
        [
            Subtitle.None
            //Subtitle.Picture,
            //Subtitle.Text
        ];

        public static Padding[] Paddings =
        [
            Padding.NoPadding,
            Padding.WithPadding
        ];

        public static ScalingBehavior[] ScalingBehaviors =
        [
            ScalingBehavior.ScaleAndPad
            //ScalingBehavior.Crop,
            //ScalingBehavior.Stretch
        ];

        public static VideoScanKind[] VideoScanKinds =
        [
            VideoScanKind.Progressive
            //VideoScanKind.Interlaced
        ];

        public static InputFormat[] InputFormats =
        [
            // // // example format that requires colorspace filter
            // new("libx264", "yuv420p", "tv", "smpte170m", "bt709", "smpte170m"),
            // // //
            // // // // example format that requires setparams filter
            // new("libx264", "yuv420p", string.Empty, string.Empty, string.Empty, string.Empty),
            // // //
            // // // // new("libx264", "yuvj420p"),
            // new("libx264", "yuv420p10le"),
            // // // // new("libx264", "yuv444p10le"),
            // // //
            // // // // new("mpeg1video", "yuv420p"),
            // // // //
            // new("mpeg2video", "yuv420p"),
            // //
            new InputFormat("libx265", "yuv420p"),
            new InputFormat("libx265", "yuv420p10le")
            //
            // new("mpeg4", "yuv420p"),
            //
            // new("libvpx-vp9", "yuv420p"),
            // new("libvpx-vp9", "yuv420p10le"),
            //
            // // // new("libaom-av1", "yuv420p")
            // // // av1    yuv420p10le    51
            // //
            // new("msmpeg4v2", "yuv420p"),
            // new("msmpeg4v3", "yuv420p")
            //
            // // wmv3    yuv420p    1
        ];

        public static Resolution[] Resolutions =
        [
            new Resolution { Width = 1920, Height = 1080 },
            new Resolution { Width = 1280, Height = 720 },
            new Resolution { Width = 640, Height = 480 }
        ];

        public static FFmpegProfileBitDepth[] BitDepths =
        [
            FFmpegProfileBitDepth.EightBit,
            FFmpegProfileBitDepth.TenBit
        ];

        public static FFmpegProfileVideoFormat[] VideoFormats =
        [
            FFmpegProfileVideoFormat.H264,
            FFmpegProfileVideoFormat.Hevc
            // FFmpegProfileVideoFormat.Mpeg2Video
        ];

        public static HardwareAccelerationKind[] TestAccelerations =
        [
            HardwareAccelerationKind.None,
            //HardwareAccelerationKind.Nvenc,
            HardwareAccelerationKind.Vaapi
            //HardwareAccelerationKind.Qsv,
            // HardwareAccelerationKind.VideoToolbox,
            // HardwareAccelerationKind.Amf
        ];

        public static StreamingMode[] StreamingModes =
        [
            StreamingMode.TransportStream,
            //StreamingMode.HttpLiveStreamingSegmenter,
            StreamingMode.HttpLiveStreamingSegmenterV2
        ];

        public static string[] FilesToTest => [string.Empty];
    }

    [Test]
    [Combinatorial]
    public async Task TranscodeSong(
        [ValueSource(typeof(TestData), nameof(TestData.Watermarks))]
        Watermark watermark,
        [ValueSource(typeof(TestData), nameof(TestData.Resolutions))]
        Resolution profileResolution,
        [ValueSource(typeof(TestData), nameof(TestData.BitDepths))]
        FFmpegProfileBitDepth profileBitDepth,
        [ValueSource(typeof(TestData), nameof(TestData.VideoFormats))]
        FFmpegProfileVideoFormat profileVideoFormat,
        [ValueSource(typeof(TestData), nameof(TestData.TestAccelerations))]
        HardwareAccelerationKind profileAcceleration,
        [ValueSource(typeof(TestData), nameof(TestData.StreamingModes))]
        StreamingMode streamingMode)
    {
        var localFileSystem = new LocalFileSystem(
            Substitute.For<IClient>(),
            LoggerFactory.CreateLogger<LocalFileSystem>());
        var tempFilePool = new TempFilePool();

        ImageCache mockImageCache = Substitute.For<ImageCache>(localFileSystem, tempFilePool);

        // always return the static watermark resource
        mockImageCache.GetPathForImage(
                Arg.Any<string>(),
                Arg.Is<ArtworkKind>(x => x == ArtworkKind.Watermark),
                Arg.Any<Option<int>>())
            .Returns(Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources", "ErsatzTV.png"));

        var oldService = new FFmpegProcessService(
            new FakeStreamSelector(),
            mockImageCache,
            tempFilePool,
            Substitute.For<IClient>(),
            MemoryCache,
            LoggerFactory.CreateLogger<FFmpegProcessService>());

        var service = new FFmpegLibraryProcessService(
            oldService,
            new FakeStreamSelector(),
            tempFilePool,
            new PipelineBuilderFactory(
                //new FakeNvidiaCapabilitiesFactory(),
                new HardwareCapabilitiesFactory(
                    MemoryCache,
                    new RuntimeInfo(),
                    LoggerFactory.CreateLogger<HardwareCapabilitiesFactory>()),
                LoggerFactory.CreateLogger<PipelineBuilderFactory>()),
            Substitute.For<IConfigElementRepository>(),
            LoggerFactory.CreateLogger<FFmpegLibraryProcessService>());

        var songVideoGenerator = new SongVideoGenerator(tempFilePool, mockImageCache, service);

        var channel = new Channel(Guid.NewGuid())
        {
            Number = "1",
            FFmpegProfile = FFmpegProfile.New("test", profileResolution) with
            {
                HardwareAcceleration = profileAcceleration,
                VideoFormat = profileVideoFormat,
                AudioFormat = FFmpegProfileAudioFormat.Aac,
                DeinterlaceVideo = true,
                BitDepth = profileBitDepth
            },
            StreamingMode = streamingMode,
            SubtitleMode = ChannelSubtitleMode.None
        };

        string file = Path.Combine(TestContext.CurrentContext.TestDirectory, Path.Combine("Resources", "song.mp3"));
        var songVersion = new MediaVersion
        {
            MediaFiles = [new MediaFile { Path = file }],
            Streams = []
        };

        var song = new Song
        {
            SongMetadata =
            [
                new SongMetadata
                {
                    Title = "Song Title",
                    Artists = ["Song Artist"],
                    AlbumArtists = [],
                    Artwork = []
                }
            ],
            MediaVersions = [songVersion]
        };

        (string videoPath, MediaVersion videoVersion) = await songVideoGenerator.GenerateSongVideo(
            song,
            channel,
            None, // playout item watermark
            None, // global watermark
            ExecutableName("ffmpeg"),
            ExecutableName("ffprobe"),
            CancellationToken.None);

        IMetadataRepository metadataRepository = Substitute.For<IMetadataRepository>();
        metadataRepository.When(x => x.UpdateStatistics(Arg.Any<MediaItem>(), Arg.Any<MediaVersion>(), Arg.Any<bool>()))
            .Do(
                x =>
                {
                    MediaVersion version = x.Arg<MediaVersion>();
                    if (version.Streams.Any(s => s.MediaStreamKind == MediaStreamKind.Video && s.AttachedPic == false))
                    {
                        version.MediaFiles = videoVersion.MediaFiles;
                        videoVersion = version;
                    }
                    else
                    {
                        version.MediaFiles = songVersion.MediaFiles;
                        songVersion = version;
                    }
                });

        var localStatisticsProvider = new LocalStatisticsProvider(
            metadataRepository,
            new LocalFileSystem(Substitute.For<IClient>(), LoggerFactory.CreateLogger<LocalFileSystem>()),
            Substitute.For<IClient>(),
            LoggerFactory.CreateLogger<LocalStatisticsProvider>());

        await localStatisticsProvider.RefreshStatistics(ExecutableName("ffmpeg"), ExecutableName("ffprobe"), song);

        DateTimeOffset now = DateTimeOffset.Now;

        Command process = await service.ForPlayoutItem(
            ExecutableName("ffmpeg"),
            ExecutableName("ffprobe"),
            false,
            channel,
            videoVersion,
            new MediaItemAudioVersion(song, songVersion),
            videoPath,
            file,
            _ => Task.FromResult(new List<ErsatzTV.Core.Domain.Subtitle>()),
            string.Empty,
            string.Empty,
            string.Empty,
            ChannelSubtitleMode.None,
            now,
            now + TimeSpan.FromSeconds(3),
            now,
            Option<ChannelWatermark>.None,
            GetWatermark(watermark),
            VaapiDriver.RadeonSI,
            "/dev/dri/renderD128",
            Option<int>.None,
            false,
            FillerKind.None,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(3),
            0,
            None,
            false,
            _ => { });

        // Console.WriteLine($"ffmpeg arguments {process.Arguments}");

        await TranscodeAndVerify(
            process,
            profileResolution,
            profileBitDepth,
            profileVideoFormat,
            profileAcceleration,
            VaapiDriver.RadeonSI,
            localStatisticsProvider,
            streamingMode,
            () => videoVersion);
    }

    [Test]
    [Combinatorial]
    public async Task Transcode(
        [ValueSource(typeof(TestData), nameof(TestData.FilesToTest))]
        string fileToTest,
        [ValueSource(typeof(TestData), nameof(TestData.InputFormats))]
        InputFormat inputFormat,
        [ValueSource(typeof(TestData), nameof(TestData.Resolutions))]
        Resolution profileResolution,
        [ValueSource(typeof(TestData), nameof(TestData.BitDepths))]
        FFmpegProfileBitDepth profileBitDepth,
        [ValueSource(typeof(TestData), nameof(TestData.Paddings))]
        Padding padding,
        [ValueSource(typeof(TestData), nameof(TestData.ScalingBehaviors))]
        ScalingBehavior scalingBehavior,
        [ValueSource(typeof(TestData), nameof(TestData.VideoScanKinds))]
        VideoScanKind videoScanKind,
        [ValueSource(typeof(TestData), nameof(TestData.Watermarks))]
        Watermark watermark,
        [ValueSource(typeof(TestData), nameof(TestData.Subtitles))]
        Subtitle subtitle,
        [ValueSource(typeof(TestData), nameof(TestData.VideoFormats))]
        FFmpegProfileVideoFormat profileVideoFormat,
        [ValueSource(typeof(TestData), nameof(TestData.TestAccelerations))]
        HardwareAccelerationKind profileAcceleration,
        [ValueSource(typeof(TestData), nameof(TestData.StreamingModes))]
        StreamingMode streamingMode)
    {
        string file = fileToTest;
        if (string.IsNullOrWhiteSpace(file))
        {
            // some formats don't support interlaced content (mpeg1video, msmpeg4v2, msmpeg4v3)
            // others (libx265, any 10-bit) are unlikely to have interlaced content, so don't bother testing
            if (inputFormat.Encoder is "mpeg1video" or "msmpeg4v2" or "msmpeg4v3" or "libx265" ||
                inputFormat.PixelFormat.Contains("10"))
            {
                if (videoScanKind == VideoScanKind.Interlaced)
                {
                    Assert.Inconclusive(
                        $"{inputFormat.Encoder}/{inputFormat.PixelFormat} does not support interlaced content");
                    return;
                }
            }

            string name = GetStringSha256Hash($"{inputFormat}_{videoScanKind}_{padding}_{scalingBehavior}_{subtitle}");

            file = Path.Combine(TestContext.CurrentContext.TestDirectory, $"{name}.mkv");
            if (!File.Exists(file))
            {
                await GenerateTestFile(inputFormat, padding, scalingBehavior, videoScanKind, subtitle, file);
            }
        }

        var v = new MediaVersion
        {
            MediaFiles = [new MediaFile { Path = file }],
            Streams = []
        };

        IMetadataRepository? metadataRepository = Substitute.For<IMetadataRepository>();
        metadataRepository
            .When(r => r.UpdateStatistics(Arg.Any<MediaItem>(), Arg.Any<MediaVersion>(), Arg.Any<bool>()))
            .Do(
                args =>
                {
                    MediaVersion? version = args.Arg<MediaVersion>();
                    version.MediaFiles = v.MediaFiles;
                    v = version;
                });

        var localStatisticsProvider = new LocalStatisticsProvider(
            metadataRepository,
            new LocalFileSystem(Substitute.For<IClient>(), LoggerFactory.CreateLogger<LocalFileSystem>()),
            Substitute.For<IClient>(),
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

        if (videoScanKind == VideoScanKind.Interlaced)
        {
            v.VideoScanKind.Should().Be(VideoScanKind.Interlaced, file);
        }

        var subtitleStreams = v.Streams
            .Filter(s => s.MediaStreamKind == MediaStreamKind.Subtitle)
            .ToList();

        var subtitles = new List<ErsatzTV.Core.Domain.Subtitle>();

        if (subtitle != Subtitle.None)
        {
            foreach (MediaStream stream in subtitleStreams)
            {
                var s = new ErsatzTV.Core.Domain.Subtitle
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
        }

        DateTimeOffset now = DateTimeOffset.Now;

        Option<ChannelWatermark> channelWatermark = GetWatermark(watermark);

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

        void PipelineAction(FFmpegPipeline pipeline)
        {
            // validate pipeline matches expectations (at a high level)

            ComplexFilter complexFilter = pipeline.PipelineSteps.OfType<ComplexFilter>().First();
            FilterChain filterChain = complexFilter.FilterChain;

            if (profileBitDepth == FFmpegProfileBitDepth.TenBit)
                // process.Arguments.Contains("=nv12") &&
                // !process.Arguments.Contains("format=nv12,format=p010le[") &&
                // !process.Arguments.Contains("hwdownload,format=nv12,subtitle") &&
                // !process.Arguments.Contains("format=nv12,hwupload_cuda[st]") &&
                // !process.Arguments.Contains("format=nv12,hwupload_cuda[wm]"))
            {
                var videoFilters = string.Join(",", filterChain.VideoFilterSteps.Map(f => f.Filter));
                var pixelFormatFilters = string.Join(",", filterChain.PixelFormatFilterSteps.Map(f => f.Filter));
                if (videoFilters.Contains("nv12") || pixelFormatFilters.Contains("nv12") &&
                    !pixelFormatFilters.EndsWith("format=nv12,format=p010le"))
                {
                    // Assert.Fail("10-bit shouldn't use NV12!");
                }
            }

            bool hasDeinterlaceFilter = filterChain.VideoFilterSteps.Any(
                s => s is YadifFilter or YadifCudaFilter or DeinterlaceQsvFilter or DeinterlaceVaapiFilter);

            hasDeinterlaceFilter.Should().Be(videoScanKind == VideoScanKind.Interlaced);

            bool hasScaling = filterChain.VideoFilterSteps.Filter(
                    s => s is ScaleFilter or ScaleCudaFilter or ScaleQsvFilter or ScaleVaapiFilter)
                .Filter(s => s is not ScaleCudaFilter cuda || !cuda.Filter.Contains("scale_cuda=format="))
                .Any();

            // TODO: sometimes scaling is used for pixel format, so this is harder to assert the absence
            if (profileResolution.Width != 1920 && profileResolution.Width != 640)
            {
                hasScaling.Should().BeTrue();
            }

            // TODO: bit depth

            bool hasPadding = filterChain.VideoFilterSteps.Any(s => s is PadFilter);

            // TODO: optimize out padding
            // hasPadding.Should().Be(padding == Padding.WithPadding);
            if (padding is Padding.WithPadding && scalingBehavior is not ScalingBehavior.Crop)
            {
                hasPadding.Should().BeTrue();
            }

            bool hasCrop = filterChain.VideoFilterSteps.Any(s => s is CropFilter);
            if (scalingBehavior is ScalingBehavior.Crop)
            {
                hasCrop.Should().BeTrue();
            }

            bool hasSubtitleFilters =
                filterChain.VideoFilterSteps.Any(s => s is SubtitlesFilter) ||
                filterChain.SubtitleOverlayFilterSteps.Any(
                    s => s is OverlaySubtitleFilter
                        or OverlaySubtitleCudaFilter
                        or OverlaySubtitleQsvFilter
                        or OverlaySubtitleVaapiFilter);

            hasSubtitleFilters.Should().Be(subtitle != Subtitle.None);

            bool hasWatermarkFilters = filterChain.WatermarkOverlayFilterSteps.Any(
                s => s is OverlayWatermarkFilter or OverlayWatermarkCudaFilter or OverlayWatermarkQsvFilter);

            hasWatermarkFilters.Should().Be(watermark != Watermark.None);
        }

        FFmpegLibraryProcessService service = GetService();

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
                    DeinterlaceVideo = true,
                    BitDepth = profileBitDepth,
                    ScalingBehavior = scalingBehavior
                },
                StreamingMode = streamingMode,
                SubtitleMode = subtitleMode
            },
            v,
            new MediaItemAudioVersion(null, v),
            file,
            file,
            _ => subtitles.AsTask(),
            string.Empty,
            string.Empty,
            string.Empty,
            subtitleMode,
            now,
            now + TimeSpan.FromSeconds(3),
            now,
            Option<ChannelWatermark>.None,
            channelWatermark,
            VaapiDriver.RadeonSI,
            "/dev/dri/renderD128",
            Option<int>.None,
            false,
            FillerKind.None,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(3),
            0,
            None,
            false,
            PipelineAction);

        // Console.WriteLine($"ffmpeg arguments {string.Join(" ", process.StartInfo.ArgumentList)}");

        await TranscodeAndVerify(
            process,
            profileResolution,
            profileBitDepth,
            profileVideoFormat,
            profileAcceleration,
            VaapiDriver.RadeonSI,
            localStatisticsProvider,
            streamingMode,
            () => v);
    }

    private Option<ChannelWatermark> GetWatermark(Watermark watermark)
    {
        switch (watermark)
        {
            case Watermark.None:
                break;
            case Watermark.IntermittentOpaque:
                return new ChannelWatermark
                {
                    ImageSource = ChannelWatermarkImageSource.Custom,
                    Mode = ChannelWatermarkMode.Intermittent,
                    // TODO: how do we make sure this actually appears
                    FrequencyMinutes = 1,
                    DurationSeconds = 2,
                    Opacity = 100
                };
            case Watermark.IntermittentTransparent:
                return new ChannelWatermark
                {
                    ImageSource = ChannelWatermarkImageSource.Custom,
                    Mode = ChannelWatermarkMode.Intermittent,
                    // TODO: how do we make sure this actually appears
                    FrequencyMinutes = 1,
                    DurationSeconds = 2,
                    Opacity = 80
                };
            case Watermark.PermanentOpaqueScaled:
                return new ChannelWatermark
                {
                    ImageSource = ChannelWatermarkImageSource.Custom,
                    Mode = ChannelWatermarkMode.Permanent,
                    Opacity = 100,
                    Size = WatermarkSize.Scaled,
                    WidthPercent = 15
                };
            case Watermark.PermanentOpaqueActualSize:
                return new ChannelWatermark
                {
                    ImageSource = ChannelWatermarkImageSource.Custom,
                    Mode = ChannelWatermarkMode.Permanent,
                    Opacity = 100,
                    Size = WatermarkSize.ActualSize
                };
            case Watermark.PermanentTransparentScaled:
                return new ChannelWatermark
                {
                    ImageSource = ChannelWatermarkImageSource.Custom,
                    Mode = ChannelWatermarkMode.Permanent,
                    Opacity = 80,
                    Size = WatermarkSize.Scaled,
                    WidthPercent = 15
                };
            case Watermark.PermanentTransparentActualSize:
                return new ChannelWatermark
                {
                    ImageSource = ChannelWatermarkImageSource.Custom,
                    Mode = ChannelWatermarkMode.Permanent,
                    Opacity = 80,
                    Size = WatermarkSize.ActualSize
                };
        }

        return Option<ChannelWatermark>.None;
    }

    private static async Task GenerateTestFile(
        InputFormat inputFormat,
        Padding padding,
        ScalingBehavior scalingBehavior,
        VideoScanKind videoScanKind,
        Subtitle subtitle,
        string file)
    {
        string resolution = (scalingBehavior, padding) switch
        {
            (ScalingBehavior.Crop, Padding.NoPadding) => "1920x1080",
            // TODO: (ScalingBehavior.Crop, Padding.WithPadding) => "632x480",
            (ScalingBehavior.Stretch or ScalingBehavior.ScaleAndPad, Padding.WithPadding) => "1920x1060",
            _ => "1920x1080"
        };

        string videoFilter = videoScanKind == VideoScanKind.Interlaced
            ? "-vf interlace=scan=tff:lowpass=complex"
            : string.Empty;
        string flags = videoScanKind == VideoScanKind.Interlaced ? "-field_order tt -flags +ildct+ilme" : string.Empty;

        string colorRange = !string.IsNullOrWhiteSpace(inputFormat.ColorRange)
            ? $" -color_range {inputFormat.ColorRange}"
            : string.Empty;

        string colorSpace = !string.IsNullOrWhiteSpace(inputFormat.ColorSpace)
            ? $" -colorspace {inputFormat.ColorSpace}"
            : string.Empty;

        string colorTransfer = !string.IsNullOrWhiteSpace(inputFormat.ColorTransfer)
            ? $" -color_trc {inputFormat.ColorTransfer}"
            : string.Empty;

        string colorPrimaries = !string.IsNullOrWhiteSpace(inputFormat.ColorPrimaries)
            ? $" -color_primaries {inputFormat.ColorPrimaries}"
            : string.Empty;

        var args =
            $"-y -f lavfi -i anoisesrc=color=brown -f lavfi -i testsrc=duration=1:size={resolution}:rate=30 {videoFilter} -c:a aac -c:v {inputFormat.Encoder}{colorRange}{colorSpace}{colorTransfer}{colorPrimaries} -shortest -pix_fmt {inputFormat.PixelFormat} -strict -2 {flags} {file}";
        BufferedCommandResult p1 = await Cli.Wrap(ExecutableName("ffmpeg"))
            .WithArguments(args)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

        string output = string.IsNullOrWhiteSpace(p1.StandardOutput)
            ? p1.StandardError
            : p1.StandardOutput;

        p1.ExitCode.Should().Be(0, output);

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

                BufferedCommandResult p2 = await new Command(ExecutableName("mkvmerge"))
                    .WithArguments(
                        $"-o {tempFileName} {sourceFile} --field-order 0:{(videoScanKind == VideoScanKind.Interlaced ? '1' : '0')} {subPath}")
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteBufferedAsync();

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

                await SetInterlacedFlag(tempFileName, sourceFile, file, videoScanKind == VideoScanKind.Interlaced);

                File.Move(tempFileName, file, true);
                break;
        }
    }

    private static async Task SetInterlacedFlag(string tempFileName, string sourceFile, string file, bool interlaced)
    {
        var p = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ExecutableName("mkvpropedit"),
                Arguments = $"{tempFileName} --edit track:v1 --set interlaced={(interlaced ? '1' : '0')}"
            }
        };

        p.Start();
        await p.WaitForExitAsync();
        // ReSharper disable once MethodHasAsyncOverload
        p.WaitForExit();
        if (p.ExitCode != 0)
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

        p.ExitCode.Should().Be(0);
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

    private static FFmpegLibraryProcessService GetService()
    {
        IImageCache? imageCache = Substitute.For<IImageCache>();

        // always return the static watermark resource
        imageCache.GetPathForImage(
                Arg.Any<string>(),
                Arg.Is<ArtworkKind>(x => x == ArtworkKind.Watermark),
                Arg.Any<Option<int>>())
            .Returns(Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources", "ErsatzTV.png"));

        var oldService = new FFmpegProcessService(
            new FakeStreamSelector(),
            imageCache,
            Substitute.For<ITempFilePool>(),
            Substitute.For<IClient>(),
            MemoryCache,
            LoggerFactory.CreateLogger<FFmpegProcessService>());

        var service = new FFmpegLibraryProcessService(
            oldService,
            new FakeStreamSelector(),
            Substitute.For<ITempFilePool>(),
            new PipelineBuilderFactory(
                //new FakeNvidiaCapabilitiesFactory(),
                new HardwareCapabilitiesFactory(
                    MemoryCache,
                    new RuntimeInfo(),
                    LoggerFactory.CreateLogger<HardwareCapabilitiesFactory>()),
                LoggerFactory.CreateLogger<PipelineBuilderFactory>()),
            Substitute.For<IConfigElementRepository>(),
            LoggerFactory.CreateLogger<FFmpegLibraryProcessService>());

        return service;
    }

    private async Task TranscodeAndVerify(
        Command process,
        Resolution profileResolution,
        FFmpegProfileBitDepth profileBitDepth,
        FFmpegProfileVideoFormat profileVideoFormat,
        HardwareAccelerationKind profileAcceleration,
        VaapiDriver vaapiDriver,
        ILocalStatisticsProvider localStatisticsProvider,
        StreamingMode streamingMode,
        Func<MediaVersion> getFinalMediaVersion)
    {
        string[] unsupportedMessages =
        {
            "No support for codec",
            "No usable",
            "Provided device doesn't support",
            "Current pixel format is unsupported"
        };

        var sb = new StringBuilder();
        var timeoutSignal = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        string tempFile = Path.GetTempFileName();
        try
        {
            CommandResult result;

            try
            {
                result = await process
                    .WithStandardOutputPipe(PipeTarget.ToFile(tempFile))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(sb))
                    .ExecuteAsync(timeoutSignal.Token);

                // var arguments = string.Join(
                //     ' ',
                //     process.Arguments.Split(" ").Map(a => a.Contains('[') ? $"\"{a}\"" : a));
                //
                // Log.Logger.Debug(arguments);
            }
            catch (OperationCanceledException)
            {
                var arguments = string.Join(
                    ' ',
                    process.Arguments.Split(" ").Map(a => a.Contains('[') ? $"\"{a}\"" : a));

                Assert.Fail($"Transcode failure (timeout): ffmpeg {arguments}");
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
                var arguments = string.Join(
                    ' ',
                    process.Arguments.Split(" ").Map(a => a.Contains('[') ? $"\"{a}\"" : a));

                Assert.Fail($"Transcode failure: ffmpeg {arguments}");
            }
            else
            {
                var arguments = string.Join(
                    ' ',
                    process.Arguments.Split(" ").Map(a => a.Contains('[') ? $"\"{a}\"" : a));

                result.ExitCode.Should().Be(0, error + Environment.NewLine + arguments);
                if (result.ExitCode == 0)
                {
                    Console.WriteLine(process.Arguments);
                }
            }

            // additional checks on resulting file
            await localStatisticsProvider.RefreshStatistics(
                ExecutableName("ffmpeg"),
                ExecutableName("ffprobe"),
                new Movie
                {
                    MediaVersions =
                    [
                        new MediaVersion
                        {
                            MediaFiles = [new MediaFile { Path = tempFile }]
                        }
                    ]
                });

            MediaVersion v = getFinalMediaVersion();

            // verify de-interlace
            v.VideoScanKind.Should().NotBe(VideoScanKind.Interlaced);

            // verify resolution
            v.Height.Should().Be(profileResolution.Height);
            v.Width.Should().Be(profileResolution.Width);

            foreach (MediaStream videoStream in v.Streams.Filter(s => s.MediaStreamKind == MediaStreamKind.Video))
            {
                // verify pixel format
                string expectedPixelFormat = (profileBitDepth, streamingMode) switch
                {
                    //(FFmpegProfileBitDepth.TenBit, StreamingMode.HttpLiveStreamingSegmenterV2) => PixelFormat.RGB555LE,
                    (FFmpegProfileBitDepth.TenBit, _) => PixelFormat.YUV420P10LE,
                    _ => PixelFormat.YUV420P
                };

                videoStream.PixelFormat.Should().Be(expectedPixelFormat);

                // verify colors
                var colorParams = new ColorParams(
                    videoStream.ColorRange,
                    videoStream.ColorSpace,
                    videoStream.ColorTransfer,
                    videoStream.ColorPrimaries);

                // AMF doesn't seem to set this metadata properly
                // MPEG2Video doesn't always seem to set this properly
                // RADEONSI driver doesn't set this properly
                // NUT doesn't set this properly
                if (profileAcceleration != HardwareAccelerationKind.Amf &&
                    profileVideoFormat != FFmpegProfileVideoFormat.Mpeg2Video &&
                    (profileAcceleration != HardwareAccelerationKind.Vaapi || vaapiDriver != VaapiDriver.RadeonSI) &&
                    streamingMode != StreamingMode.HttpLiveStreamingSegmenterV2)
                {
                    colorParams.IsBt709.Should().BeTrue($"{colorParams}");
                }
            }
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    private class FakeStreamSelector : IFFmpegStreamSelector
    {
        public Task<MediaStream> SelectVideoStream(MediaVersion version) =>
            version.Streams.First(s => s.MediaStreamKind == MediaStreamKind.Video).AsTask();

        public Task<Option<MediaStream>> SelectAudioStream(
            MediaItemAudioVersion version,
            StreamingMode streamingMode,
            Channel channel,
            string preferredAudioLanguage,
            string preferredAudioTitle) =>
            Optional(version.MediaVersion.Streams.FirstOrDefault(s => s.MediaStreamKind == MediaStreamKind.Audio))
                .AsTask();

        public Task<Option<ErsatzTV.Core.Domain.Subtitle>> SelectSubtitleStream(
            List<ErsatzTV.Core.Domain.Subtitle> subtitles,
            Channel channel,
            string preferredSubtitleLanguage,
            ChannelSubtitleMode subtitleMode) =>
            subtitles.HeadOrNone().AsTask();
    }

    private static string ExecutableName(string baseName) =>
        OperatingSystem.IsWindows() ? $"{baseName}.exe" : baseName;
}
