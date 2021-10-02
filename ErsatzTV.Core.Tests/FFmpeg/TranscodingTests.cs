using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
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
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Tests.FFmpeg
{
    [TestFixture]
    public class TranscodingTests
    {
        private class TestData
        {
            public static string[] InputCodecs =
            {
                "h264",
                "mpeg2video",
                "hevc",
                "mpeg4"
            };

            public static string[] InputPixelFormats =
            {
                "yuv420p",
                "yuv420p10le",
                "yuv444p",
                "yuv444p10le"
            };
            
            public static Resolution[] Resolutions =
            {
                new() { Width = 1920, Height = 1080 },
                new() { Width = 1280, Height = 720 }
            };

            public static string[] Codecs =
            {
                "libx264",
                "libx265"
            };

            public static HardwareAccelerationKind[] AccelerationKinds =
            {
                HardwareAccelerationKind.None
            };
        }

        [Test, Combinatorial]
        public async Task Transcode(
            [ValueSource(typeof(TestData), nameof(TestData.InputCodecs))] string inputCodec,
            [ValueSource(typeof(TestData), nameof(TestData.InputPixelFormats))] string inputPixelFormat,
            [ValueSource(typeof(TestData), nameof(TestData.Resolutions))] Resolution profileResolution,
            [ValueSource(typeof(TestData), nameof(TestData.Codecs))] string profileCodec,
            [ValueSource(typeof(TestData), nameof(TestData.AccelerationKinds))] HardwareAccelerationKind profileAcceleration)
        {
            string file = Path.Combine(TestContext.CurrentContext.TestDirectory, "test.mkv");

            var args = $"-y -f lavfi -i anullsrc=channel_layout=stereo:sample_rate=44100 -f lavfi -i testsrc=duration=3:size=1920x1080:rate=30 -c:a aac -c:v {inputCodec} -shortest -pix_fmt {inputPixelFormat} -strict -2 {file}";
            var p1 = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = args
                }
            };

            p1.Start();
            await p1.WaitForExitAsync();
            p1.ExitCode.Should().Be(0);

            var service = new FFmpegProcessService(
                new FFmpegPlaybackSettingsCalculator(),
                new FakeStreamSelector(),
                new Mock<IImageCache>().Object);

            MediaVersion v = new MediaVersion();
            
            var metadataRepository = new Mock<IMetadataRepository>();
            metadataRepository
                .Setup(r => r.UpdateLocalStatistics(It.IsAny<int>(), It.IsAny<MediaVersion>(), It.IsAny<bool>()))
                .Callback<int, MediaVersion, bool>((_, version, _) => v = version);

            var localStatisticsProvider = new LocalStatisticsProvider(
                metadataRepository.Object,
                new LocalFileSystem(),
                new Mock<ILogger<LocalStatisticsProvider>>().Object);

            await localStatisticsProvider.RefreshStatistics(
                "/usr/bin/ffprobe",
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

            Process process = await service.ForPlayoutItem(
                "/usr/bin/ffmpeg",
                false,
                new Channel(Guid.NewGuid())
                {
                    FFmpegProfile = FFmpegProfile.New("test", profileResolution) with
                    {
                        HardwareAcceleration = profileAcceleration,
                        VideoCodec = profileCodec
                    },
                    StreamingMode = StreamingMode.TransportStream
                },
                v,
                file,
                now,
                now,
                None,
                None);

            // process.StartInfo.RedirectStandardError = true;
            
            process.Start().Should().BeTrue();

            await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // string err = await process.StandardError.ReadToEndAsync();

            process.ExitCode.Should().Be(0);
        }

        private class FakeStreamSelector : IFFmpegStreamSelector
        {
            public Task<MediaStream> SelectVideoStream(Channel channel, MediaVersion version) =>
                version.Streams.First(s => s.MediaStreamKind == MediaStreamKind.Video).AsTask();

            public Task<Option<MediaStream>> SelectAudioStream(Channel channel, MediaVersion version) =>
                Optional(version.Streams.First(s => s.MediaStreamKind == MediaStreamKind.Audio)).AsTask();
        }
    }
}
