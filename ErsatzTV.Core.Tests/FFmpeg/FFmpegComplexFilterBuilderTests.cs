using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.FFmpeg.State;
using FluentAssertions;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.FFmpeg;

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

            Option<FFmpegComplexFilter> result = builder.Build(false, 0, 0, 0, 1, false);

            result.IsNone.Should().BeTrue();
        }

        [Test]
        public void Should_Return_Audio_Filter_With_AudioDuration()
        {
            var duration = TimeSpan.FromMilliseconds(1000.1);
            FFmpegComplexFilterBuilder builder = new FFmpegComplexFilterBuilder()
                .WithAlignedAudio(duration);

            Option<FFmpegComplexFilter> result = builder.Build(false, 0, 0, 0, 1, false);

            result.IsSome.Should().BeTrue();
            result.IfSome(
                filter =>
                {
                    filter.ComplexFilter.Should().Be("[0:1]apad=whole_dur=1000.1ms[a]");
                    filter.AudioLabel.Should().Be("[a]");
                    filter.VideoLabel.Should().Be("0:0");
                });
        }
            
        [Test]
        // this needs to be a culture where '.' is a group separator
        [SetCulture("it-IT")] 
        public void Should_Return_Audio_Filter_With_AudioDuration_Decimal()
        {
            var duration = TimeSpan.FromMilliseconds(1000.1);
            FFmpegComplexFilterBuilder builder = new FFmpegComplexFilterBuilder()
                .WithAlignedAudio(duration);

            Option<FFmpegComplexFilter> result = builder.Build(false, 0, 0, 0, 1, false);

            result.IsSome.Should().BeTrue();
            result.IfSome(
                filter =>
                {
                    filter.ComplexFilter.Should().Be("[0:1]apad=whole_dur=1000.1ms[a]");
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

            Option<FFmpegComplexFilter> result = builder.Build(false, 0, 0, 0, 1, false);

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

            Option<FFmpegComplexFilter> result = builder.Build(false, 0, 0, 0, 1, false);

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
            WatermarkLocation.BottomLeft,
            false,
            100,
            "[0:0][1:v]overlay=x=134:y=H-h-54[v]",
            "0:1",
            "[v]")]
        [TestCase(
            false,
            false,
            false,
            WatermarkLocation.BottomRight,
            false,
            100,
            "[0:0][1:v]overlay=x=W-w-134:y=H-h-54[v]",
            "0:1",
            "[v]")]
        [TestCase(
            false,
            false,
            false,
            WatermarkLocation.TopLeft,
            false,
            100,
            "[0:0][1:v]overlay=x=134:y=54[v]",
            "0:1",
            "[v]")]
        [TestCase(
            false,
            false,
            false,
            WatermarkLocation.TopRight,
            false,
            100,
            "[0:0][1:v]overlay=x=W-w-134:y=54[v]",
            "0:1",
            "[v]")]
        [TestCase(
            false,
            false,
            true,
            WatermarkLocation.TopLeft,
            false,
            100,
            "[1:v]format=yuva420p|yuva444p|yuva422p|rgba|abgr|bgra|gbrap|ya8,fade=in:st=300:d=1:alpha=1:enable='between(t,0,314)',fade=out:st=315:d=1:alpha=1:enable='between(t,301,899)',fade=in:st=900:d=1:alpha=1:enable='between(t,316,914)',fade=out:st=915:d=1:alpha=1:enable='between(t,901,1499)',fade=in:st=1500:d=1:alpha=1:enable='between(t,916,1514)',fade=out:st=1515:d=1:alpha=1:enable='between(t,1501,2099)',fade=in:st=2100:d=1:alpha=1:enable='between(t,1516,2114)',fade=out:st=2115:d=1:alpha=1:enable='between(t,2101,2699)',fade=in:st=2700:d=1:alpha=1:enable='between(t,2116,2714)',fade=out:st=2715:d=1:alpha=1:enable='between(t,2701,3300)'[wmp];[0:0][wmp]overlay=x=134:y=54,format=nv12[v]",
            "0:1",
            "[v]")]
        [TestCase(
            false,
            false,
            false,
            WatermarkLocation.TopLeft,
            true,
            100,
            "[1:v]scale=384:-1[wmp];[0:0][wmp]overlay=x=134:y=54[v]",
            "0:1",
            "[v]")]
        [TestCase(
            false,
            false,
            false,
            WatermarkLocation.TopLeft,
            false,
            90,
            "[1:v]format=yuva420p|yuva444p|yuva422p|rgba|abgr|bgra|gbrap|ya8,colorchannelmixer=aa=0.90[wmp];[0:0][wmp]overlay=x=134:y=54[v]",
            "0:1",
            "[v]")]
        [TestCase(
            false,
            true,
            false,
            WatermarkLocation.TopLeft,
            false,
            100,
            "[0:0]yadif=1[vt];[vt][1:v]overlay=x=134:y=54[v]",
            "0:1",
            "[v]")]
        [TestCase(
            false,
            true,
            false,
            WatermarkLocation.TopLeft,
            true,
            100,
            "[0:0]yadif=1[vt];[1:v]scale=384:-1[wmp];[vt][wmp]overlay=x=134:y=54[v]",
            "0:1",
            "[v]")]
        [TestCase(
            true,
            true,
            false,
            WatermarkLocation.TopLeft,
            false,
            100,
            "[0:1]apad=whole_dur=3300000ms[a];[0:0]yadif=1[vt];[vt][1:v]overlay=x=134:y=54[v]",
            "[a]",
            "[v]")]
        [TestCase(
            true,
            false,
            false,
            WatermarkLocation.TopLeft,
            false,
            100,
            "[0:1]apad=whole_dur=3300000ms[a];[0:0][1:v]overlay=x=134:y=54[v]",
            "[a]",
            "[v]")]
        public void Should_Return_Watermark(
            bool alignAudio,
            bool deinterlace,
            bool intermittent,
            WatermarkLocation location,
            bool scaled,
            int opacity,
            string expectedVideoFilter,
            string expectedAudioLabel,
            string expectedVideoLabel)
        {
            var watermark = new ChannelWatermark
            {
                Mode = intermittent
                    ? ChannelWatermarkMode.Intermittent
                    : ChannelWatermarkMode.Permanent,
                DurationSeconds = intermittent ? 15 : 0,
                FrequencyMinutes = intermittent ? 10 : 0,
                Location = location,
                Size = scaled ? WatermarkSize.Scaled : WatermarkSize.ActualSize,
                WidthPercent = scaled ? 20 : 0,
                Opacity = opacity,
                HorizontalMarginPercent = 7,
                VerticalMarginPercent = 5
            };

            Option<List<FadePoint>> maybeFadePoints = watermark.Mode == ChannelWatermarkMode.Intermittent
                ? Some(
                    WatermarkCalculator.CalculateFadePoints(
                        new DateTimeOffset(2022, 01, 31, 12, 25, 0, TimeSpan.FromHours(-5)),
                        TimeSpan.Zero,
                        TimeSpan.FromMinutes(55),
                        TimeSpan.Zero,
                        watermark.FrequencyMinutes,
                        watermark.DurationSeconds))
                : None;
                
            FFmpegComplexFilterBuilder builder = new FFmpegComplexFilterBuilder()
                .WithWatermark(
                    Some(watermark),
                    maybeFadePoints,
                    new Resolution { Width = 1920, Height = 1080 },
                    None)
                .WithDeinterlace(deinterlace)
                .WithAlignedAudio(alignAudio ? Some(TimeSpan.FromMinutes(55)) : None);

            Option<FFmpegComplexFilter> result = builder.Build(false, 0, 0, 0, 1, false);

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
        [TestCase(
            false,
            false,
            false,
            WatermarkLocation.BottomLeft,
            false,
            100,
            "[0:0]scale_cuda=format=yuv420p[vt];[1:v]format=yuva420p,hwupload_cuda[wmp];[vt][wmp]overlay_cuda=x=134:y=H-h-54[v]",
            "0:1",
            "[v]",
            false)]
        [TestCase(
            false,
            false,
            false,
            WatermarkLocation.BottomLeft,
            false,
            100,
            "[0:0]scale_cuda=1920:1080,setsar=1,hwdownload,format=nv12,format=yuv420p,hwupload_cuda[vt];[1:v]format=yuva420p,hwupload_cuda[wmp];[vt][wmp]overlay_cuda=x=134:y=H-h-54,hwupload[v]",
            "0:1",
            "[v]",
            true)]
        [TestCase(
            false,
            false,
            true,
            WatermarkLocation.TopLeft,
            false,
            100,
            "[0:0]scale_cuda=format=yuv420p[vt];[1:v]format=yuva420p,fade=in:st=300:d=1:alpha=1:enable='between(t,0,314)',fade=out:st=315:d=1:alpha=1:enable='between(t,301,899)',fade=in:st=900:d=1:alpha=1:enable='between(t,316,914)',fade=out:st=915:d=1:alpha=1:enable='between(t,901,1499)',fade=in:st=1500:d=1:alpha=1:enable='between(t,916,1514)',fade=out:st=1515:d=1:alpha=1:enable='between(t,1501,2099)',fade=in:st=2100:d=1:alpha=1:enable='between(t,1516,2114)',fade=out:st=2115:d=1:alpha=1:enable='between(t,2101,2699)',fade=in:st=2700:d=1:alpha=1:enable='between(t,2116,2714)',fade=out:st=2715:d=1:alpha=1:enable='between(t,2701,3300)',hwupload_cuda[wmp];[vt][wmp]overlay_cuda=x=134:y=54[v]",
            "0:1",
            "[v]",
            false)]
        [TestCase(
            false,
            false,
            true,
            WatermarkLocation.TopLeft,
            false,
            100,
            "[0:0]scale_cuda=1920:1080,setsar=1,hwdownload,format=nv12,format=yuv420p,hwupload_cuda[vt];[1:v]format=yuva420p,fade=in:st=300:d=1:alpha=1:enable='between(t,0,314)',fade=out:st=315:d=1:alpha=1:enable='between(t,301,899)',fade=in:st=900:d=1:alpha=1:enable='between(t,316,914)',fade=out:st=915:d=1:alpha=1:enable='between(t,901,1499)',fade=in:st=1500:d=1:alpha=1:enable='between(t,916,1514)',fade=out:st=1515:d=1:alpha=1:enable='between(t,1501,2099)',fade=in:st=2100:d=1:alpha=1:enable='between(t,1516,2114)',fade=out:st=2115:d=1:alpha=1:enable='between(t,2101,2699)',fade=in:st=2700:d=1:alpha=1:enable='between(t,2116,2714)',fade=out:st=2715:d=1:alpha=1:enable='between(t,2701,3300)',hwupload_cuda[wmp];[vt][wmp]overlay_cuda=x=134:y=54,hwupload[v]",
            "0:1",
            "[v]",
            true)]
        [TestCase(
            false,
            false,
            false,
            WatermarkLocation.TopLeft,
            true,
            100,
            "[0:0]scale_cuda=format=yuv420p[vt];[1:v]format=yuva420p,scale=384:-1,hwupload_cuda[wmp];[vt][wmp]overlay_cuda=x=134:y=54[v]",
            "0:1",
            "[v]",
            false)]
        [TestCase(
            false,
            false,
            false,
            WatermarkLocation.TopLeft,
            true,
            100,
            "[0:0]scale_cuda=1920:1080,setsar=1,hwdownload,format=nv12,format=yuv420p,hwupload_cuda[vt];[1:v]format=yuva420p,scale=384:-1,hwupload_cuda[wmp];[vt][wmp]overlay_cuda=x=134:y=54,hwupload[v]",
            "0:1",
            "[v]",
            true)]
        [TestCase(
            false,
            false,
            false,
            WatermarkLocation.TopLeft,
            false,
            90,
            "[0:0]scale_cuda=format=yuv420p[vt];[1:v]format=yuva420p,colorchannelmixer=aa=0.90,hwupload_cuda[wmp];[vt][wmp]overlay_cuda=x=134:y=54[v]",
            "0:1",
            "[v]",
            false)]
        [TestCase(
            false,
            false,
            false,
            WatermarkLocation.TopLeft,
            false,
            90,
            "[0:0]scale_cuda=1920:1080,setsar=1,hwdownload,format=nv12,format=yuv420p,hwupload_cuda[vt];[1:v]format=yuva420p,colorchannelmixer=aa=0.90,hwupload_cuda[wmp];[vt][wmp]overlay_cuda=x=134:y=54,hwupload[v]",
            "0:1",
            "[v]",
            true)]            
        // TODO: do we need these anymore? interlaced content that isn't handled by mpeg2_cuvid?
        // [TestCase(
        //     false,
        //     true,
        //     false,
        //     WatermarkLocation.TopLeft,
        //     false,
        //     100,
        //     "[0:0]yadif=1[vt];[vt][1:v]overlay=x=134:y=54[v]",
        //     "0:1",
        //     "[v]")]
        // [TestCase(
        //     false,
        //     true,
        //     false,
        //     WatermarkLocation.TopLeft,
        //     true,
        //     100,
        //     "[0:0]yadif=1[vt];[1:v]scale=384:-1[wmp];[vt][wmp]overlay=x=134:y=54[v]",
        //     "0:1",
        //     "[v]")]
        // [TestCase(
        //     true,
        //     true,
        //     false,
        //     WatermarkLocation.TopLeft,
        //     false,
        //     100,
        //     "[0:1]apad=whole_dur=3300000ms[a];[0:0]yadif=1[vt];[vt][1:v]overlay=x=134:y=54[v]",
        //     "[a]",
        //     "[v]")]
        [TestCase(
            true,
            false,
            false,
            WatermarkLocation.TopLeft,
            false,
            100,
            "[0:1]apad=whole_dur=3300000ms[a];[0:0]scale_cuda=format=yuv420p[vt];[1:v]format=yuva420p,hwupload_cuda[wmp];[vt][wmp]overlay_cuda=x=134:y=54[v]",
            "[a]",
            "[v]",
            false)]
        [TestCase(
            true,
            false,
            false,
            WatermarkLocation.TopLeft,
            false,
            100,
            "[0:1]apad=whole_dur=3300000ms[a];[0:0]scale_cuda=1920:1080,setsar=1,hwdownload,format=nv12,format=yuv420p,hwupload_cuda[vt];[1:v]format=yuva420p,hwupload_cuda[wmp];[vt][wmp]overlay_cuda=x=134:y=54,hwupload[v]",
            "[a]",
            "[v]",
            true)]
        public void Should_Return_NVENC_Watermark(
            bool alignAudio,
            bool deinterlace,
            bool intermittent,
            WatermarkLocation location,
            bool scaled,
            int opacity,
            string expectedVideoFilter,
            string expectedAudioLabel,
            string expectedVideoLabel,
            bool scaledSource)
        {
            var watermark = new ChannelWatermark
            {
                Mode = intermittent
                    ? ChannelWatermarkMode.Intermittent
                    : ChannelWatermarkMode.Permanent,
                DurationSeconds = intermittent ? 15 : 0,
                FrequencyMinutes = intermittent ? 10 : 0,
                Location = location,
                Size = scaled ? WatermarkSize.Scaled : WatermarkSize.ActualSize,
                WidthPercent = scaled ? 20 : 0,
                Opacity = opacity,
                HorizontalMarginPercent = 7,
                VerticalMarginPercent = 5
            };

            Option<List<FadePoint>> maybeFadePoints = watermark.Mode == ChannelWatermarkMode.Intermittent
                ? Some(
                    WatermarkCalculator.CalculateFadePoints(
                        new DateTimeOffset(2022, 01, 31, 12, 25, 0, TimeSpan.FromHours(-5)),
                        TimeSpan.Zero,
                        TimeSpan.FromMinutes(55),
                        TimeSpan.Zero,
                        watermark.FrequencyMinutes,
                        watermark.DurationSeconds))
                : None;
                
            FFmpegComplexFilterBuilder builder = new FFmpegComplexFilterBuilder()
                .WithHardwareAcceleration(HardwareAccelerationKind.Nvenc)
                .WithWatermark(
                    Some(watermark),
                    maybeFadePoints,
                    new Resolution { Width = 1920, Height = 1080 },
                    None)
                .WithDeinterlace(deinterlace)
                .WithAlignedAudio(alignAudio ? Some(TimeSpan.FromMinutes(55)) : None);

            if (scaledSource)
            {
                builder = builder.WithScaling(new Resolution { Width = 1920, Height = 1080 });
            }

            Option<FFmpegComplexFilter> result = builder.Build(false, 0, 0, 0, 1, false);

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
            "[0:0]deinterlace_qsv,scale_qsv=w=1920:h=1000,setsar=1[v]",
            "[v]")]
        [TestCase(
            true,
            false,
            true,
            "[0:0]deinterlace_qsv,setsar=1,hwdownload,format=nv12,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload=extra_hw_frames=64[v]",
            "[v]")]
        [TestCase(
            true,
            true,
            true,
            "[0:0]deinterlace_qsv,scale_qsv=w=1920:h=1000,setsar=1,hwdownload,format=nv12,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload=extra_hw_frames=64[v]",
            "[v]")]
        [TestCase(
            false,
            true,
            false,
            "[0:0]scale_qsv=w=1920:h=1000,setsar=1[v]",
            "[v]")]
        [TestCase(
            false,
            false,
            true,
            "[0:0]setsar=1,hwdownload,format=nv12,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload=extra_hw_frames=64[v]",
            "[v]")]
        [TestCase(
            false,
            true,
            true,
            "[0:0]scale_qsv=w=1920:h=1000,setsar=1,hwdownload,format=nv12,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload=extra_hw_frames=64[v]",
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

            Option<FFmpegComplexFilter> result = builder.Build(false, 0, 0, 0, 1, false);

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
        [TestCase(true, false, false, "[0:0]yadif_cuda[v]", "[v]")]
        [TestCase(
            true,
            true,
            false,
            "[0:0]yadif_cuda,scale_cuda=1920:1000,setsar=1[v]",
            "[v]")]
        [TestCase(
            true,
            false,
            true,
            "[0:0]yadif_cuda,setsar=1,hwdownload,format=nv12,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
            "[v]")]
        [TestCase(
            true,
            true,
            true,
            "[0:0]yadif_cuda,scale_cuda=1920:1000,setsar=1,hwdownload,format=nv12,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
            "[v]")]
        [TestCase(
            false,
            true,
            false,
            "[0:0]scale_cuda=1920:1000,setsar=1[v]",
            "[v]")]
        [TestCase(
            false,
            false,
            true,
            "[0:0]setsar=1,hwdownload,format=nv12,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
            "[v]")]
        [TestCase(
            false,
            true,
            true,
            "[0:0]scale_cuda=1920:1000,setsar=1,hwdownload,format=nv12,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
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
                .WithDeinterlace(deinterlace)
                .WithInputPixelFormat("h264");

            if (scale)
            {
                builder = builder.WithScaling(new Resolution { Width = 1920, Height = 1000 });
            }

            if (pad)
            {
                builder = builder.WithBlackBars(new Resolution { Width = 1920, Height = 1080 });
            }

            Option<FFmpegComplexFilter> result = builder.Build(false, 0, 0, 0, 1, false);

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
            "[0:0]deinterlace_vaapi,scale_vaapi=format=nv12:w=1920:h=1000,setsar=1[v]",
            "[v]")]
        [TestCase(
            "h264",
            true,
            false,
            true,
            "[0:0]deinterlace_vaapi,setsar=1,hwdownload,format=nv12|vaapi,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
            "[v]")]
        [TestCase(
            "h264",
            true,
            true,
            true,
            "[0:0]deinterlace_vaapi,scale_vaapi=format=nv12:w=1920:h=1000,setsar=1,hwdownload,format=nv12|vaapi,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
            "[v]")]
        [TestCase(
            "h264",
            false,
            true,
            false,
            "[0:0]scale_vaapi=format=nv12:w=1920:h=1000,setsar=1[v]",
            "[v]")]
        [TestCase(
            "h264",
            false,
            false,
            true,
            "[0:0]setsar=1,hwdownload,format=nv12|vaapi,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
            "[v]")]
        [TestCase(
            "h264",
            false,
            true,
            true,
            "[0:0]scale_vaapi=format=nv12:w=1920:h=1000,setsar=1,hwdownload,format=nv12|vaapi,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
            "[v]")]
        [TestCase("mpeg4", true, false, false, "[0:0]hwupload,deinterlace_vaapi[v]", "[v]")]
        [TestCase(
            "mpeg4",
            true,
            true,
            false,
            "[0:0]hwupload,deinterlace_vaapi,scale_vaapi=format=nv12:w=1920:h=1000,setsar=1[v]",
            "[v]")]
        [TestCase(
            "mpeg4",
            true,
            false,
            true,
            "[0:0]hwupload,deinterlace_vaapi,setsar=1,hwdownload,format=nv12|vaapi,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
            "[v]")]
        [TestCase(
            "mpeg4",
            true,
            true,
            true,
            "[0:0]hwupload,deinterlace_vaapi,scale_vaapi=format=nv12:w=1920:h=1000,setsar=1,hwdownload,format=nv12|vaapi,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
            "[v]")]
        [TestCase(
            "mpeg4",
            false,
            true,
            false,
            "[0:0]hwupload,scale_vaapi=format=nv12:w=1920:h=1000,setsar=1[v]",
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
            "[0:0]hwupload,scale_vaapi=format=nv12:w=1920:h=1000,setsar=1,hwdownload,format=nv12|vaapi,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,hwupload[v]",
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

            Option<FFmpegComplexFilter> result = builder.Build(false, 0, 0, 0, 1, false);

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