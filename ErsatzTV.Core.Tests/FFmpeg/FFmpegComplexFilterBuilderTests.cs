using System;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using FluentAssertions;
using LanguageExt;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.FFmpeg
{
    [TestFixture]
    public class FFmpegComplexFilterBuilderTests
    {
        [TestFixture]
        public class Build
        {
            [Test]
            public void Should_Return_None_With_No_Filters()
            {
                var builder = new FFmpegComplexFilterBuilder();

                Option<FFmpegComplexFilter> result = builder.Build();

                result.IsNone.Should().BeTrue();
            }

            [Test]
            public void Should_Return_Audio_Filter_With_AudioDuration()
            {
                var duration = TimeSpan.FromMinutes(54);
                FFmpegComplexFilterBuilder builder = new FFmpegComplexFilterBuilder()
                    .WithAlignedAudio(duration);

                Option<FFmpegComplexFilter> result = builder.Build();

                result.IsSome.Should().BeTrue();
                result.IfSome(
                    filter =>
                    {
                        filter.ComplexFilter.Should().Be($"[0:a]apad=whole_dur={duration.TotalMilliseconds}ms[a]");
                        filter.AudioLabel.Should().Be("[a]");
                        filter.VideoLabel.Should().Be("0:v");
                    });
            }

            [Test]
            public void Should_Return_Audio_And_Video_Filter()
            {
                var duration = TimeSpan.FromMinutes(54);
                FFmpegComplexFilterBuilder builder = new FFmpegComplexFilterBuilder()
                    .WithAlignedAudio(duration)
                    .WithDeinterlace(true);

                Option<FFmpegComplexFilter> result = builder.Build();

                result.IsSome.Should().BeTrue();
                result.IfSome(
                    filter =>
                    {
                        filter.ComplexFilter.Should().Be(
                            $"[0:a]apad=whole_dur={duration.TotalMilliseconds}ms[a];[0:v]yadif=1[v]");
                        filter.AudioLabel.Should().Be("[a]");
                        filter.VideoLabel.Should().Be("[v]");
                    });
            }

            [Test]
            [TestCase(true, false, false, "[0:v]yadif=1[v]", "[v]")]
            [TestCase(true, true, false, "[0:v]yadif=1,scale=1920:1000:flags=fast_bilinear,setsar=1[v]", "[v]")]
            [TestCase(true, false, true, "[0:v]yadif=1,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2[v]", "[v]")]
            [TestCase(
                true,
                true,
                true,
                "[0:v]yadif=1,scale=1920:1000:flags=fast_bilinear,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2[v]",
                "[v]")]
            [TestCase(false, true, false, "[0:v]scale=1920:1000:flags=fast_bilinear,setsar=1[v]", "[v]")]
            [TestCase(false, false, true, "[0:v]setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2[v]", "[v]")]
            [TestCase(
                false,
                true,
                true,
                "[0:v]scale=1920:1000:flags=fast_bilinear,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2[v]",
                "[v]")]
            public void Should_Return_Software_Video_Filter(
                bool deinterlace,
                bool scale,
                bool pad,
                string expectedVideoFilter,
                string expectedVideoLabel)
            {
                FFmpegComplexFilterBuilder builder = new FFmpegComplexFilterBuilder()
                    .WithDeinterlace(deinterlace);

                if (scale)
                {
                    builder = builder.WithScaling(new Resolution { Width = 1920, Height = 1000 });
                }

                if (pad)
                {
                    builder = builder.WithBlackBars(new Resolution { Width = 1920, Height = 1080 });
                }

                Option<FFmpegComplexFilter> result = builder.Build();

                result.IsSome.Should().BeTrue();
                result.IfSome(
                    filter =>
                    {
                        filter.ComplexFilter.Should().Be(expectedVideoFilter);
                        filter.AudioLabel.Should().Be("0:a");
                        filter.VideoLabel.Should().Be(expectedVideoLabel);
                    });
            }

            [Test]
            [TestCase(true, false, false, "[0:v]deinterlace_qsv[v]", "[v]")]
            [TestCase(
                true,
                true,
                false,
                "[0:v]deinterlace_qsv,scale_qsv=w=1920:h=1000,hwdownload,format=nv12,setsar=1,hwupload=extra_hw_frames=64[v]",
                "[v]")]
            [TestCase(
                true,
                false,
                true,
                "[0:v]deinterlace_qsv,hwdownload,format=nv12,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload=extra_hw_frames=64[v]",
                "[v]")]
            [TestCase(
                true,
                true,
                true,
                "[0:v]deinterlace_qsv,scale_qsv=w=1920:h=1000,hwdownload,format=nv12,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload=extra_hw_frames=64[v]",
                "[v]")]
            [TestCase(false, true, false, "[0:v]scale_qsv=w=1920:h=1000,hwdownload,format=nv12,setsar=1,hwupload=extra_hw_frames=64[v]", "[v]")]
            [TestCase(
                false,
                false,
                true,
                "[0:v]hwdownload,format=nv12,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload=extra_hw_frames=64[v]",
                "[v]")]
            [TestCase(
                false,
                true,
                true,
                "[0:v]scale_qsv=w=1920:h=1000,hwdownload,format=nv12,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload=extra_hw_frames=64[v]",
                "[v]")]
            public void Should_Return_QSV_Video_Filter(
                bool deinterlace,
                bool scale,
                bool pad,
                string expectedVideoFilter,
                string expectedVideoLabel)
            {
                FFmpegComplexFilterBuilder builder = new FFmpegComplexFilterBuilder()
                    .WithHardwareAcceleration(HardwareAccelerationKind.Qsv)
                    .WithDeinterlace(deinterlace);

                if (scale)
                {
                    builder = builder.WithScaling(new Resolution { Width = 1920, Height = 1000 });
                }

                if (pad)
                {
                    builder = builder.WithBlackBars(new Resolution { Width = 1920, Height = 1080 });
                }

                Option<FFmpegComplexFilter> result = builder.Build();

                result.IsSome.Should().BeTrue();
                result.IfSome(
                    filter =>
                    {
                        filter.ComplexFilter.Should().Be(expectedVideoFilter);
                        filter.AudioLabel.Should().Be("0:a");
                        filter.VideoLabel.Should().Be(expectedVideoLabel);
                    });
            }

            [Test]
            // TODO: get yadif_cuda working in docker
            // [TestCase(true, false, false, "[0:v]yadif_cuda[v]", "[v]")]
            // [TestCase(
            //     true,
            //     true,
            //     false,
            //     "[0:v]yadif_cuda,scale_npp=1920:1000:format=yuv420p,hwdownload,setsar=1,hwupload[v]",
            //     "[v]")]
            // [TestCase(
            //     true,
            //     false,
            //     true,
            //     "[0:v]yadif_cuda,hwdownload,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
            //     "[v]")]
            // [TestCase(
            //     true,
            //     true,
            //     true,
            //     "[0:v]yadif_cuda,scale_npp=1920:1000:format=yuv420p,hwdownload,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
            //     "[v]")]
            [TestCase(
                true,
                true,
                false,
                "[0:v]scale_npp=1920:1000:format=yuv420p,hwdownload,setsar=1,hwupload[v]",
                "[v]")]
            [TestCase(
                true,
                false,
                true,
                "[0:v]hwdownload,format=nv12,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
                "[v]")]
            [TestCase(
                true,
                true,
                true,
                "[0:v]scale_npp=1920:1000:format=yuv420p,hwdownload,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
                "[v]")]
            [TestCase(
                false,
                true,
                false,
                "[0:v]scale_npp=1920:1000:format=yuv420p,hwdownload,setsar=1,hwupload[v]",
                "[v]")]
            [TestCase(
                false,
                false,
                true,
                "[0:v]hwdownload,format=nv12,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
                "[v]")]
            [TestCase(
                false,
                true,
                true,
                "[0:v]scale_npp=1920:1000:format=yuv420p,hwdownload,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
                "[v]")]
            public void Should_Return_NVENC_Video_Filter(
                bool deinterlace,
                bool scale,
                bool pad,
                string expectedVideoFilter,
                string expectedVideoLabel)
            {
                FFmpegComplexFilterBuilder builder = new FFmpegComplexFilterBuilder()
                    .WithHardwareAcceleration(HardwareAccelerationKind.Nvenc)
                    .WithDeinterlace(deinterlace);

                if (scale)
                {
                    builder = builder.WithScaling(new Resolution { Width = 1920, Height = 1000 });
                }

                if (pad)
                {
                    builder = builder.WithBlackBars(new Resolution { Width = 1920, Height = 1080 });
                }

                Option<FFmpegComplexFilter> result = builder.Build();

                result.IsSome.Should().BeTrue();
                result.IfSome(
                    filter =>
                    {
                        filter.ComplexFilter.Should().Be(expectedVideoFilter);
                        filter.AudioLabel.Should().Be("0:a");
                        filter.VideoLabel.Should().Be(expectedVideoLabel);
                    });
            }

            [Test]
            [TestCase(true, false, false, "[0:v]deinterlace_vaapi[v]", "[v]")]
            [TestCase(
                true,
                true,
                false,
                "[0:v]deinterlace_vaapi,scale_vaapi=w=1920:h=1000,hwdownload,setsar=1,hwupload[v]",
                "[v]")]
            [TestCase(
                true,
                false,
                true,
                "[0:v]deinterlace_vaapi,hwdownload,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
                "[v]")]
            [TestCase(
                true,
                true,
                true,
                "[0:v]deinterlace_vaapi,scale_vaapi=w=1920:h=1000,hwdownload,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
                "[v]")]
            [TestCase(false, true, false, "[0:v]scale_vaapi=w=1920:h=1000,hwdownload,setsar=1,hwupload[v]", "[v]")]
            [TestCase(
                false,
                false,
                true,
                "[0:v]hwdownload,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
                "[v]")]
            [TestCase(
                false,
                true,
                true,
                "[0:v]scale_vaapi=w=1920:h=1000,hwdownload,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
                "[v]")]
            public void Should_Return_VAAPI_Video_Filter(
                bool deinterlace,
                bool scale,
                bool pad,
                string expectedVideoFilter,
                string expectedVideoLabel)
            {
                FFmpegComplexFilterBuilder builder = new FFmpegComplexFilterBuilder()
                    .WithHardwareAcceleration(HardwareAccelerationKind.Vaapi)
                    .WithDeinterlace(deinterlace);

                if (scale)
                {
                    builder = builder.WithScaling(new Resolution { Width = 1920, Height = 1000 });
                }

                if (pad)
                {
                    builder = builder.WithBlackBars(new Resolution { Width = 1920, Height = 1080 });
                }

                Option<FFmpegComplexFilter> result = builder.Build();

                result.IsSome.Should().BeTrue();
                result.IfSome(
                    filter =>
                    {
                        filter.ComplexFilter.Should().Be(expectedVideoFilter);
                        filter.AudioLabel.Should().Be("0:a");
                        filter.VideoLabel.Should().Be(expectedVideoLabel);
                    });
            }
        }
    }
}
