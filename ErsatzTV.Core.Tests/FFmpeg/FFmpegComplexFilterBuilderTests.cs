using System;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using FluentAssertions;
using LanguageExt;
using NUnit.Framework;
using static LanguageExt.Prelude;

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

                Option<FFmpegComplexFilter> result = builder.Build(0, 1);

                result.IsNone.Should().BeTrue();
            }

            [Test]
            public void Should_Return_Audio_Filter_With_AudioDuration()
            {
                var duration = TimeSpan.FromMinutes(54);
                FFmpegComplexFilterBuilder builder = new FFmpegComplexFilterBuilder()
                    .WithAlignedAudio(duration);

                Option<FFmpegComplexFilter> result = builder.Build(0, 1);

                result.IsSome.Should().BeTrue();
                result.IfSome(
                    filter =>
                    {
                        filter.ComplexFilter.Should().Be($"[0:1]apad=whole_dur={duration.TotalMilliseconds}ms[a]");
                        filter.AudioLabel.Should().Be("[a]");
                        filter.VideoLabel.Should().Be("0:0");
                    });
            }

            [Test]
            public void Should_Return_Audio_And_Video_Filter()
            {
                var duration = TimeSpan.FromMinutes(54);
                FFmpegComplexFilterBuilder builder = new FFmpegComplexFilterBuilder()
                    .WithAlignedAudio(duration)
                    .WithDeinterlace(true);

                Option<FFmpegComplexFilter> result = builder.Build(0, 1);

                result.IsSome.Should().BeTrue();
                result.IfSome(
                    filter =>
                    {
                        filter.ComplexFilter.Should().Be(
                            $"[0:1]apad=whole_dur={duration.TotalMilliseconds}ms[a];[0:0]yadif=1[v]");
                        filter.AudioLabel.Should().Be("[a]");
                        filter.VideoLabel.Should().Be("[v]");
                    });
            }

            [Test]
            [TestCase(true, false, false, "[0:0]yadif=1[v]", "[v]")]
            [TestCase(true, true, false, "[0:0]yadif=1,scale=1920:1000:flags=fast_bilinear,setsar=1[v]", "[v]")]
            [TestCase(true, false, true, "[0:0]yadif=1,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2[v]", "[v]")]
            [TestCase(
                true,
                true,
                true,
                "[0:0]yadif=1,scale=1920:1000:flags=fast_bilinear,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2[v]",
                "[v]")]
            [TestCase(false, true, false, "[0:0]scale=1920:1000:flags=fast_bilinear,setsar=1[v]", "[v]")]
            [TestCase(false, false, true, "[0:0]setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2[v]", "[v]")]
            [TestCase(
                false,
                true,
                true,
                "[0:0]scale=1920:1000:flags=fast_bilinear,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2[v]",
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

                Option<FFmpegComplexFilter> result = builder.Build(0, 1);

                result.IsSome.Should().BeTrue();
                result.IfSome(
                    filter =>
                    {
                        filter.ComplexFilter.Should().Be(expectedVideoFilter);
                        filter.AudioLabel.Should().Be("0:1");
                        filter.VideoLabel.Should().Be(expectedVideoLabel);
                    });
            }

            [Test]
            [TestCase(
                false,
                false,
                false,
                ChannelWatermarkLocation.BottomLeft,
                false,
                100,
                "[0:0][1:v]overlay=x=134:y=H-h-54[v]",
                "0:1",
                "[v]")]
            [TestCase(
                false,
                false,
                false,
                ChannelWatermarkLocation.BottomRight,
                false,
                100,
                "[0:0][1:v]overlay=x=W-w-134:y=H-h-54[v]",
                "0:1",
                "[v]")]
            [TestCase(
                false,
                false,
                false,
                ChannelWatermarkLocation.TopLeft,
                false,
                100,
                "[0:0][1:v]overlay=x=134:y=54[v]",
                "0:1",
                "[v]")]
            [TestCase(
                false,
                false,
                false,
                ChannelWatermarkLocation.TopRight,
                false,
                100,
                "[0:0][1:v]overlay=x=W-w-134:y=54[v]",
                "0:1",
                "[v]")]
            [TestCase(
                false,
                false,
                true,
                ChannelWatermarkLocation.TopLeft,
                false,
                100,
                "[0:0][1:v]overlay=x=134:y=54:enable='lt(mod(mod(time(0),60*60),10*60),15)'[v]",
                "0:1",
                "[v]")]
            [TestCase(
                false,
                false,
                false,
                ChannelWatermarkLocation.TopLeft,
                true,
                100,
                "[1:v]scale=384:-1[wmp];[0:0][wmp]overlay=x=134:y=54[v]",
                "0:1",
                "[v]")]
            [TestCase(
                false,
                false,
                false,
                ChannelWatermarkLocation.TopLeft,
                false,
                90,
                "[1:v]format=yuva420p|yuva444p|yuva422p|rgba|abgr|bgra|gbrap|ya8,colorchannelmixer=aa=0.90[wmp];[0:0][wmp]overlay=x=134:y=54[v]",
                "0:1",
                "[v]")]
            [TestCase(
                false,
                true,
                false,
                ChannelWatermarkLocation.TopLeft,
                false,
                100,
                "[0:0]yadif=1[vt];[vt][1:v]overlay=x=134:y=54[v]",
                "0:1",
                "[v]")]
            [TestCase(
                false,
                true,
                false,
                ChannelWatermarkLocation.TopLeft,
                true,
                100,
                "[0:0]yadif=1[vt];[1:v]scale=384:-1[wmp];[vt][wmp]overlay=x=134:y=54[v]",
                "0:1",
                "[v]")]
            [TestCase(
                true,
                true,
                false,
                ChannelWatermarkLocation.TopLeft,
                false,
                100,
                "[0:1]apad=whole_dur=3300000ms[a];[0:0]yadif=1[vt];[vt][1:v]overlay=x=134:y=54[v]",
                "[a]",
                "[v]")]
            [TestCase(
                true,
                false,
                false,
                ChannelWatermarkLocation.TopLeft,
                false,
                100,
                "[0:1]apad=whole_dur=3300000ms[a];[0:0][1:v]overlay=x=134:y=54[v]",
                "[a]",
                "[v]")]
            public void Should_Return_Watermark(
                bool alignAudio,
                bool deinterlace,
                bool intermittent,
                ChannelWatermarkLocation location,
                bool scaled,
                int opacity,
                string expectedVideoFilter,
                string expectedAudioLabel,
                string expectedVideoLabel)
            {
                FFmpegComplexFilterBuilder builder = new FFmpegComplexFilterBuilder()
                    .WithWatermark(
                        Some(
                            new ChannelWatermark
                            {
                                Mode = intermittent
                                    ? ChannelWatermarkMode.Intermittent
                                    : ChannelWatermarkMode.Permanent,
                                DurationSeconds = intermittent ? 15 : 0,
                                FrequencyMinutes = intermittent ? 10 : 0,
                                Location = location,
                                Size = scaled ? ChannelWatermarkSize.Scaled : ChannelWatermarkSize.ActualSize,
                                WidthPercent = scaled ? 20 : 0,
                                Opacity = opacity,
                                HorizontalMarginPercent = 7,
                                VerticalMarginPercent = 5
                            }),
                        new Resolution { Width = 1920, Height = 1080 })
                    .WithDeinterlace(deinterlace)
                    .WithAlignedAudio(alignAudio ? Some(TimeSpan.FromMinutes(55)) : None);

                Option<FFmpegComplexFilter> result = builder.Build(0, 1);

                result.IsSome.Should().BeTrue();
                result.IfSome(
                    filter =>
                    {
                        filter.ComplexFilter.Should().Be(expectedVideoFilter);
                        filter.AudioLabel.Should().Be(expectedAudioLabel);
                        filter.VideoLabel.Should().Be(expectedVideoLabel);
                    });
            }

            [Test]
            [TestCase(true, false, false, "[0:0]deinterlace_qsv[v]", "[v]")]
            [TestCase(
                true,
                true,
                false,
                "[0:0]deinterlace_qsv,scale_qsv=w=1920:h=1000,hwdownload,format=nv12,setsar=1,hwupload=extra_hw_frames=64[v]",
                "[v]")]
            [TestCase(
                true,
                false,
                true,
                "[0:0]deinterlace_qsv,hwdownload,format=nv12,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload=extra_hw_frames=64[v]",
                "[v]")]
            [TestCase(
                true,
                true,
                true,
                "[0:0]deinterlace_qsv,scale_qsv=w=1920:h=1000,hwdownload,format=nv12,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload=extra_hw_frames=64[v]",
                "[v]")]
            [TestCase(
                false,
                true,
                false,
                "[0:0]scale_qsv=w=1920:h=1000,hwdownload,format=nv12,setsar=1,hwupload=extra_hw_frames=64[v]",
                "[v]")]
            [TestCase(
                false,
                false,
                true,
                "[0:0]hwdownload,format=nv12,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload=extra_hw_frames=64[v]",
                "[v]")]
            [TestCase(
                false,
                true,
                true,
                "[0:0]scale_qsv=w=1920:h=1000,hwdownload,format=nv12,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload=extra_hw_frames=64[v]",
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

                Option<FFmpegComplexFilter> result = builder.Build(0, 1);

                result.IsSome.Should().BeTrue();
                result.IfSome(
                    filter =>
                    {
                        filter.ComplexFilter.Should().Be(expectedVideoFilter);
                        filter.AudioLabel.Should().Be("0:1");
                        filter.VideoLabel.Should().Be(expectedVideoLabel);
                    });
            }

            [Test]
            // TODO: get yadif_cuda working in docker
            // [TestCase(true, false, false, "[0:V]yadif_cuda[v]", "[v]")]
            // [TestCase(
            //     true,
            //     true,
            //     false,
            //     "[0:V]yadif_cuda,scale_npp=1920:1000:format=yuv420p,hwdownload,setsar=1,hwupload[v]",
            //     "[v]")]
            // [TestCase(
            //     true,
            //     false,
            //     true,
            //     "[0:V]yadif_cuda,hwdownload,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
            //     "[v]")]
            // [TestCase(
            //     true,
            //     true,
            //     true,
            //     "[0:V]yadif_cuda,scale_npp=1920:1000:format=yuv420p,hwdownload,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
            //     "[v]")]
            [TestCase(
                true,
                true,
                false,
                "[0:0]hwupload_cuda,scale_npp=1920:1000,hwdownload,format=yuv420p|nv12,setsar=1,hwupload[v]",
                "[v]")]
            [TestCase(
                true,
                false,
                true,
                "[0:0]hwdownload,format=yuv420p|nv12,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
                "[v]")]
            [TestCase(
                true,
                true,
                true,
                "[0:0]hwupload_cuda,scale_npp=1920:1000,hwdownload,format=yuv420p|nv12,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
                "[v]")]
            [TestCase(
                false,
                true,
                false,
                "[0:0]hwupload_cuda,scale_npp=1920:1000,hwdownload,format=yuv420p|nv12,setsar=1,hwupload[v]",
                "[v]")]
            [TestCase(
                false,
                false,
                true,
                "[0:0]hwdownload,format=yuv420p|nv12,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
                "[v]")]
            [TestCase(
                false,
                true,
                true,
                "[0:0]hwupload_cuda,scale_npp=1920:1000,hwdownload,format=yuv420p|nv12,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
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

                Option<FFmpegComplexFilter> result = builder.Build(0, 1);

                result.IsSome.Should().BeTrue();
                result.IfSome(
                    filter =>
                    {
                        filter.ComplexFilter.Should().Be(expectedVideoFilter);
                        filter.AudioLabel.Should().Be("0:1");
                        filter.VideoLabel.Should().Be(expectedVideoLabel);
                    });
            }

            [Test]
            [TestCase("h264", true, false, false, "[0:0]deinterlace_vaapi[v]", "[v]")]
            [TestCase(
                "h264",
                true,
                true,
                false,
                "[0:0]deinterlace_vaapi,scale_vaapi=w=1920:h=1000,hwdownload,format=nv12|vaapi,setsar=1,hwupload[v]",
                "[v]")]
            [TestCase(
                "h264",
                true,
                false,
                true,
                "[0:0]deinterlace_vaapi,hwdownload,format=nv12|vaapi,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
                "[v]")]
            [TestCase(
                "h264",
                true,
                true,
                true,
                "[0:0]deinterlace_vaapi,scale_vaapi=w=1920:h=1000,hwdownload,format=nv12|vaapi,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
                "[v]")]
            [TestCase(
                "h264",
                false,
                true,
                false,
                "[0:0]scale_vaapi=w=1920:h=1000,hwdownload,format=nv12|vaapi,setsar=1,hwupload[v]",
                "[v]")]
            [TestCase(
                "h264",
                false,
                false,
                true,
                "[0:0]hwdownload,format=nv12|vaapi,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
                "[v]")]
            [TestCase(
                "h264",
                false,
                true,
                true,
                "[0:0]scale_vaapi=w=1920:h=1000,hwdownload,format=nv12|vaapi,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
                "[v]")]
            [TestCase("mpeg4", true, false, false, "[0:0]hwupload,deinterlace_vaapi[v]", "[v]")]
            [TestCase(
                "mpeg4",
                true,
                true,
                false,
                "[0:0]hwupload,deinterlace_vaapi,scale_vaapi=w=1920:h=1000,hwdownload,format=nv12|vaapi,setsar=1,hwupload[v]",
                "[v]")]
            [TestCase(
                "mpeg4",
                true,
                false,
                true,
                "[0:0]hwupload,deinterlace_vaapi,hwdownload,format=nv12|vaapi,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
                "[v]")]
            [TestCase(
                "mpeg4",
                true,
                true,
                true,
                "[0:0]hwupload,deinterlace_vaapi,scale_vaapi=w=1920:h=1000,hwdownload,format=nv12|vaapi,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
                "[v]")]
            [TestCase(
                "mpeg4",
                false,
                true,
                false,
                "[0:0]hwupload,scale_vaapi=w=1920:h=1000,hwdownload,format=nv12|vaapi,setsar=1,hwupload[v]",
                "[v]")]
            [TestCase(
                "mpeg4",
                false,
                false,
                true,
                "[0:0]setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
                "[v]")]
            [TestCase(
                "mpeg4",
                false,
                true,
                true,
                "[0:0]hwupload,scale_vaapi=w=1920:h=1000,hwdownload,format=nv12|vaapi,setsar=1,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
                "[v]")]
            public void Should_Return_VAAPI_Video_Filter(
                string codec,
                bool deinterlace,
                bool scale,
                bool pad,
                string expectedVideoFilter,
                string expectedVideoLabel)
            {
                FFmpegComplexFilterBuilder builder = new FFmpegComplexFilterBuilder()
                    .WithHardwareAcceleration(HardwareAccelerationKind.Vaapi)
                    .WithInputCodec(codec)
                    .WithDeinterlace(deinterlace);

                if (scale)
                {
                    builder = builder.WithScaling(new Resolution { Width = 1920, Height = 1000 });
                }

                if (pad)
                {
                    builder = builder.WithBlackBars(new Resolution { Width = 1920, Height = 1080 });
                }

                Option<FFmpegComplexFilter> result = builder.Build(0, 1);

                result.IsSome.Should().BeTrue();
                result.IfSome(
                    filter =>
                    {
                        filter.ComplexFilter.Should().Be(expectedVideoFilter);
                        filter.AudioLabel.Should().Be("0:1");
                        filter.VideoLabel.Should().Be(expectedVideoLabel);
                    });
            }
        }
    }
}
