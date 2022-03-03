using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Serilog;
using static LanguageExt.Prelude;

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
        PermanentOpaque,
        PermanentTransparent,
        IntermittentOpaque,
        IntermittentTransparent
        // TODO: animated vs static
    }

    private class TestData
    {
        public static Watermark[] Watermarks =
        {
            Watermark.None,
            Watermark.PermanentOpaque,
            Watermark.PermanentTransparent
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

        public static string[] SoftwareCodecs =
        {
            "libx264",
            "libx265"
        };

        public static HardwareAccelerationKind[] NoAcceleration =
        {
            HardwareAccelerationKind.None
        };

        public static string[] NvidiaCodecs =
        {
            "h264_nvenc",
            "hevc_nvenc"
        };

        public static HardwareAccelerationKind[] NvidiaAcceleration =
        {
            HardwareAccelerationKind.Nvenc
        };

        public static string[] VaapiCodecs =
        {
            "h264_vaapi",
            "hevc_vaapi"
        };

        public static HardwareAccelerationKind[] VaapiAcceleration =
        {
            HardwareAccelerationKind.Vaapi
        };

        public static string[] VideoToolboxCodecs =
        {
            "h264_videotoolbox",
            "hevc_videotoolbox"
        };

        public static HardwareAccelerationKind[] VideoToolboxAcceleration =
        {
            HardwareAccelerationKind.VideoToolbox
        };

        public static string[] QsvCodecs =
        {
            "h264_qsv",
            "hevc_qsv"
        };

        public static HardwareAccelerationKind[] QsvAcceleration =
        {
            HardwareAccelerationKind.Qsv
        };
    }

    [Test, Combinatorial]
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
            // [ValueSource(typeof(TestData), nameof(TestData.SoftwareCodecs))] string profileCodec,
            // [ValueSource(typeof(TestData), nameof(TestData.NoAcceleration))] HardwareAccelerationKind profileAcceleration)
            [ValueSource(typeof(TestData), nameof(TestData.NvidiaCodecs))] string profileCodec,
            [ValueSource(typeof(TestData), nameof(TestData.NvidiaAcceleration))] HardwareAccelerationKind profileAcceleration)
        // [ValueSource(typeof(TestData), nameof(TestData.VaapiCodecs))] string profileCodec,
        // [ValueSource(typeof(TestData), nameof(TestData.VaapiAcceleration))] HardwareAccelerationKind profileAcceleration)
        // [ValueSource(typeof(TestData), nameof(TestData.QsvCodecs))] string profileCodec,
        // [ValueSource(typeof(TestData), nameof(TestData.QsvAcceleration))] HardwareAccelerationKind profileAcceleration)
        // [ValueSource(typeof(TestData), nameof(TestData.VideoToolboxCodecs))] string profileCodec,
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
            $"{inputFormat.Encoder}_{inputFormat.PixelFormat}_{videoScanKind}_{padding}_{profileResolution}_{profileCodec}_{profileAcceleration}");

        string file = Path.Combine(TestContext.CurrentContext.TestDirectory, $"{name}.mkv");
        if (!File.Exists(file))
        {
            string resolution = padding == Padding.WithPadding ? "1920x1060" : "1920x1080";

            string videoFilter = videoScanKind == VideoScanKind.Interlaced ? "-vf tinterlace=interleave_top,fieldorder=tff" : string.Empty;
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
            LoggerFactory.CreateLogger<FFmpegProcessService>());

        var service = new FFmpegLibraryProcessService(
            oldService,
            new FFmpegPlaybackSettingsCalculator(),
            new FakeStreamSelector(),
            LoggerFactory.CreateLogger<FFmpegLibraryProcessService>());

        var v = new MediaVersion
        {
            MediaFiles = new List<MediaFile>
            {
                new() { Path = file }
            }
        };

        var metadataRepository = new Mock<IMetadataRepository>();
        metadataRepository
            .Setup(r => r.UpdateLocalStatistics(It.IsAny<MediaItem>(), It.IsAny<MediaVersion>(), It.IsAny<bool>()))
            .Callback<MediaItem, MediaVersion, bool>((_, version, _) =>
            {
                version.MediaFiles = v.MediaFiles;
                v = version;
            });

        var localStatisticsProvider = new LocalStatisticsProvider(
            metadataRepository.Object,
            new LocalFileSystem(LoggerFactory.CreateLogger<LocalFileSystem>()),
            LoggerFactory.CreateLogger<LocalStatisticsProvider>());

        await localStatisticsProvider.RefreshStatistics(
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
            case Watermark.PermanentOpaque:
                channelWatermark = new ChannelWatermark
                {
                    ImageSource = ChannelWatermarkImageSource.Custom,
                    Mode = ChannelWatermarkMode.Permanent,
                    Opacity = 100
                };
                break;
            case Watermark.PermanentTransparent:
                channelWatermark = new ChannelWatermark
                {
                    ImageSource = ChannelWatermarkImageSource.Custom,
                    Mode = ChannelWatermarkMode.Permanent,
                    Opacity = 80
                };
                break;
        }

        Process process = await service.ForPlayoutItem(
            ExecutableName("ffmpeg"),
            false,
            new Channel(Guid.NewGuid())
            {
                Number = "1",
                FFmpegProfile = FFmpegProfile.New("test", profileResolution) with
                {
                    HardwareAcceleration = profileAcceleration,
                    VideoCodec = profileCodec,
                    AudioCodec = "aac"
                },
                StreamingMode = StreamingMode.TransportStream
            },
            v,
            v,
            file,
            file,
            now,
            now + TimeSpan.FromSeconds(5),
            now,
            channelWatermark,
            VaapiDriver.Default,
            "/dev/dri/renderD128",
            false,
            FillerKind.None,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(5),
            0,
            None);

        process.StartInfo.RedirectStandardError = true;
        process.EnableRaisingEvents = true;
            
        // Console.WriteLine($"ffmpeg arguments {string.Join(" ", process.StartInfo.ArgumentList)}");

        process.Start().Should().BeTrue();

        string[] unsupportedMessages =
        {
            "No support for codec",
            "No usable",
            "Provided device doesn't support"
        };

        var errorBuffer = new StringBuilder();
            
        process.ErrorDataReceived += (_, errorLine) =>
        {
            string data = errorLine.Data ?? string.Empty;
            errorBuffer.AppendLine(data);
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        // string error = await process.StandardError.ReadToEndAsync();

        var timeoutSignal = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(timeoutSignal.Token);
            // ReSharper disable once MethodHasAsyncOverload
            process.WaitForExit();
        }
        catch (OperationCanceledException)
        {
            process.Kill();

            IEnumerable<string> quotedArgs = process.StartInfo.ArgumentList.Map(a => $"\'{a}\'");
            Assert.Fail($"Transcode failure (timeout): ffmpeg {string.Join(" ", quotedArgs)}");
            return;
        }

        var error = errorBuffer.ToString();
        bool isUnsupported = unsupportedMessages.Any(error.Contains); 

        if (profileAcceleration != HardwareAccelerationKind.None && isUnsupported)
        {
            var quotedArgs = process.StartInfo.ArgumentList.Map(a => $"\'{a}\'").ToList();
            process.ExitCode.Should().Be(1, $"Error message with successful exit code? {string.Join(" ", quotedArgs)}");
            Assert.Warn($"Unsupported on this hardware: ffmpeg {string.Join(" ", quotedArgs)}");
        }
        else if (error.Contains("Impossible to convert between"))
        {
            IEnumerable<string> quotedArgs = process.StartInfo.ArgumentList.Map(a => $"\'{a}\'");
            Assert.Fail($"Transcode failure: ffmpeg {string.Join(" ", quotedArgs)}");
        }
        else
        {
            var quotedArgs = process.StartInfo.ArgumentList.Map(a => $"\'{a}\'").ToList();
            process.ExitCode.Should().Be(0, errorBuffer + Environment.NewLine + string.Join(" ", quotedArgs));
            if (process.ExitCode == 0)
            {
                Console.WriteLine(string.Join(" ", quotedArgs));
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
        public Task<MediaStream> SelectVideoStream(Channel channel, MediaVersion version) =>
            version.Streams.First(s => s.MediaStreamKind == MediaStreamKind.Video).AsTask();

        public Task<Option<MediaStream>> SelectAudioStream(Channel channel, MediaVersion version) =>
            Optional(version.Streams.First(s => s.MediaStreamKind == MediaStreamKind.Audio)).AsTask();
    }

    private static string ExecutableName(string baseName) =>
        OperatingSystem.IsWindows() ? $"{baseName}.exe" : baseName;
}