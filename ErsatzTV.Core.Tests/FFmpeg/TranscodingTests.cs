using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Tests.FFmpeg
{
    [TestFixture]
    [Explicit]
    public class TranscodingTests
    {
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
                "yuvj420p",
                "yuv444p",
                "yuv444p10le"
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
        }

        [Test, Combinatorial]
        public async Task Transcode(
            [ValueSource(typeof(TestData), nameof(TestData.InputCodecs))]
            string inputCodec,
            [ValueSource(typeof(TestData), nameof(TestData.InputPixelFormats))]
            string inputPixelFormat,
            [ValueSource(typeof(TestData), nameof(TestData.Resolutions))]
            Resolution profileResolution,
            [Values(true, false)]
            bool pad,
            // [ValueSource(typeof(TestData), nameof(TestData.SoftwareCodecs))] string profileCodec,
            // [ValueSource(typeof(TestData), nameof(TestData.NoAcceleration))] HardwareAccelerationKind profileAcceleration)
            [ValueSource(typeof(TestData), nameof(TestData.NvidiaCodecs))] string profileCodec,
            [ValueSource(typeof(TestData), nameof(TestData.NvidiaAcceleration))] HardwareAccelerationKind profileAcceleration)
            // [ValueSource(typeof(TestData), nameof(TestData.VaapiCodecs))] string profileCodec,
            // [ValueSource(typeof(TestData), nameof(TestData.VaapiAcceleration))] HardwareAccelerationKind profileAcceleration)
        {
            string name = GetStringSha256Hash(
                $"{inputCodec}_{inputPixelFormat}_{pad}_{profileResolution}_{profileCodec}_{profileAcceleration}");

            string file = Path.Combine(TestContext.CurrentContext.TestDirectory, $"{name}.mkv");
            if (!File.Exists(file))
            {
                string resolution = pad ? "1920x1060" : "1920x1080";
                
                var args =
                    $"-y -f lavfi -i anullsrc=channel_layout=stereo:sample_rate=44100 -f lavfi -i testsrc=duration=1:size={resolution}:rate=30 -c:a aac -c:v {inputCodec} -shortest -pix_fmt {inputPixelFormat} -strict -2 {file}";
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
                // ReSharper disable once MethodHasAsyncOverload
                p1.WaitForExit();
                p1.ExitCode.Should().Be(0);
            }

            var service = new FFmpegProcessService(
                new FFmpegPlaybackSettingsCalculator(),
                new FakeStreamSelector(),
                new Mock<IImageCache>().Object,
                new Mock<ITempFilePool>().Object,
                new Mock<ILogger<FFmpegProcessService>>().Object);

            MediaVersion v = new MediaVersion();

            var metadataRepository = new Mock<IMetadataRepository>();
            metadataRepository
                .Setup(r => r.UpdateLocalStatistics(It.IsAny<MediaItem>(), It.IsAny<MediaVersion>(), It.IsAny<bool>()))
                .Callback<MediaItem, MediaVersion, bool>((_, version, _) => v = version);

            var localStatisticsProvider = new LocalStatisticsProvider(
                metadataRepository.Object,
                new LocalFileSystem(new Mock<ILogger<LocalFileSystem>>().Object),
                new Mock<ILogger<LocalStatisticsProvider>>().Object);

            await localStatisticsProvider.RefreshStatistics(
                "ffprobe",
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
                "ffmpeg",
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
                v,
                file,
                file,
                now,
                now + TimeSpan.FromSeconds(5),
                now,
                None,
                VaapiDriver.Default,
                "/dev/dri/renderD128",
                false,
                FillerKind.None,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(5));

            process.StartInfo.RedirectStandardError = true;
            
            // Console.WriteLine($"ffmpeg arguments {string.Join(" ", process.StartInfo.ArgumentList)}");

            process.Start().Should().BeTrue();

            process.BeginOutputReadLine();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            // ReSharper disable once MethodHasAsyncOverload
            process.WaitForExit();

            string[] unsupportedMessages =
            {
                "No support for codec",
                "No usable",
                "Provided device doesn't support"
            };
            
            if (profileAcceleration != HardwareAccelerationKind.None && unsupportedMessages.Any(error.Contains))
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
                IEnumerable<string> quotedArgs = process.StartInfo.ArgumentList.Map(a => $"\'{a}\'");
                process.ExitCode.Should().Be(0, error + Environment.NewLine + string.Join(" ", quotedArgs));
            }
        }
        
        private static string GetStringSha256Hash(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            using var sha = SHA256.Create();
            byte[] textData = System.Text.Encoding.UTF8.GetBytes(text);
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
    }
}
