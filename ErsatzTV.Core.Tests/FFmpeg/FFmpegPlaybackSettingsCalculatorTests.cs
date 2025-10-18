using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.FFmpeg;
using NUnit.Framework;
using Shouldly;
using MediaStream = ErsatzTV.Core.Domain.MediaStream;

namespace ErsatzTV.Core.Tests.FFmpeg;

[TestFixture]
public class FFmpegPlaybackSettingsCalculatorTests
{
    private static readonly MediaVersion TestVersion = new() { SampleAspectRatio = "1:1", Width = 1920, Height = 1080 };

    [TestFixture]
    public class CalculateSettings
    {
        [Test]
        public void Should_Not_GenPts_ForHlsSegmenter()
        {
            FFmpegProfile ffmpegProfile = TestProfile();

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.HttpLiveStreamingSegmenter,
                ffmpegProfile,
                TestVersion,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.FormatFlags.ShouldNotContain("+genpts");
        }

        [Test]
        public void Should_Not_GenPts_ForHlsSegmenterFmp4()
        {
            FFmpegProfile ffmpegProfile = TestProfile();

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.HttpLiveStreamingSegmenter,
                ffmpegProfile,
                TestVersion,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.FormatFlags.ShouldNotContain("+genpts");
        }

        [Test]
        public void Should_UseSpecifiedThreadCount_ForTransportStream()
        {
            // MPEG-TS requires realtime output which is hardcoded to a single thread
            // The thread limitation now happens in ErsatzTV.FFmpeg.Pipeline.PipelineBuilderBase
            // So the value should be passed through this playback settings process

            FFmpegProfile ffmpegProfile = TestProfile() with { ThreadCount = 7 };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                TestVersion,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.ThreadCount.ShouldBe(7);
        }

        [Test]
        public void Should_UseSpecifiedThreadCount_ForHttpLiveStreamingSegmenter()
        {
            FFmpegProfile ffmpegProfile = TestProfile() with { ThreadCount = 7 };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.HttpLiveStreamingSegmenter,
                ffmpegProfile,
                TestVersion,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.ThreadCount.ShouldBe(7);
        }

        [Test]
        public void Should_UseSpecifiedThreadCount_ForHttpLiveStreamingSegmenterFmp4()
        {
            FFmpegProfile ffmpegProfile = TestProfile() with { ThreadCount = 7 };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.HttpLiveStreamingSegmenter,
                ffmpegProfile,
                TestVersion,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.ThreadCount.ShouldBe(7);
        }

        [Test]
        public void Should_SetFormatFlags_ForTransportStream()
        {
            FFmpegProfile ffmpegProfile = TestProfile();

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                TestVersion,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            string[] expected = { "+genpts", "+discardcorrupt", "+igndts" };
            actual.FormatFlags.Count.ShouldBe(expected.Length);
            expected.ShouldBeSubsetOf(actual.FormatFlags);
        }

        [Test]
        public void Should_SetFormatFlags_ForHttpLiveStreaming()
        {
            FFmpegProfile ffmpegProfile = TestProfile();

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.HttpLiveStreamingDirect,
                ffmpegProfile,
                TestVersion,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            string[] expected = { "+genpts", "+discardcorrupt", "+igndts" };
            actual.FormatFlags.Count.ShouldBe(expected.Length);
            expected.ShouldBeSubsetOf(actual.FormatFlags);
        }

        [Test]
        public void Should_SetRealtime_ForTransportStream()
        {
            FFmpegProfile ffmpegProfile = TestProfile();

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                TestVersion,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.RealtimeOutput.ShouldBeTrue();
        }

        [Test]
        public void Should_SetRealtime_ForHttpLiveStreaming()
        {
            FFmpegProfile ffmpegProfile = TestProfile();

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.HttpLiveStreamingDirect,
                ffmpegProfile,
                TestVersion,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.RealtimeOutput.ShouldBeTrue();
        }

        [Test]
        public void Should_SetStreamSeek_When_PlaybackIsLate_ForTransportStream()
        {
            DateTimeOffset now = DateTimeOffset.Now;

            FFmpegProfile ffmpegProfile = TestProfile();

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                TestVersion,
                new MediaStream(),
                now,
                now.AddMinutes(5),
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.StreamSeek.IsSome.ShouldBeTrue();
            actual.StreamSeek.IfNone(TimeSpan.Zero).ShouldBe(TimeSpan.FromMinutes(5));
        }

        [Test]
        public void Should_SetStreamSeek_When_PlaybackIsLate_ForHttpLiveStreaming()
        {
            DateTimeOffset now = DateTimeOffset.Now;

            FFmpegProfile ffmpegProfile = TestProfile();

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.HttpLiveStreamingDirect,
                ffmpegProfile,
                TestVersion,
                new MediaStream(),
                now,
                now.AddMinutes(5),
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.StreamSeek.IsSome.ShouldBeTrue();
            actual.StreamSeek.IfNone(TimeSpan.Zero).ShouldBe(TimeSpan.FromMinutes(5));
        }

        [Test]
        public void ShouldNot_SetScaledSize_When_ContentIsCorrectSize_ForTransportStream()
        {
            FFmpegProfile ffmpegProfile = TestProfile() with
            {
                Resolution = new Resolution { Width = 1920, Height = 1080 }
            };

            // not anamorphic
            var version = new MediaVersion { Width = 1920, Height = 1080, SampleAspectRatio = "1:1" };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                version,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.ScaledSize.IsNone.ShouldBeTrue();
        }

        [Test]
        public void ShouldNot_SetScaledSize_When_ScaledSizeWouldEqualContentSize_ForTransportStream()
        {
            FFmpegProfile ffmpegProfile = TestProfile() with
            {
                Resolution = new Resolution { Width = 1920, Height = 1080 }
            };

            // not anamorphic
            var version = new MediaVersion { Width = 1918, Height = 1080, SampleAspectRatio = "1:1" };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                version,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.ScaledSize.IsNone.ShouldBeTrue();
        }

        [Test]
        public void ShouldNot_PadToDesiredResolution_When_ContentIsCorrectSize_ForTransportStream()
        {
            FFmpegProfile ffmpegProfile = TestProfile() with
            {
                Resolution = new Resolution { Width = 1920, Height = 1080 }
            };

            // not anamorphic
            var version = new MediaVersion { Width = 1920, Height = 1080, SampleAspectRatio = "1:1" };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                version,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.ScaledSize.IsNone.ShouldBeTrue();
            actual.PadToDesiredResolution.ShouldBeFalse();
        }

        [Test]
        public void Should_PadToDesiredResolution_When_UnscaledContentIsUnderSized_ForTransportStream()
        {
            FFmpegProfile ffmpegProfile = TestProfile() with
            {
                Resolution = new Resolution { Width = 1920, Height = 1080 }
            };

            // not anamorphic
            var version = new MediaVersion { Width = 1918, Height = 1080, SampleAspectRatio = "1:1" };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                version,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.ScaledSize.IsNone.ShouldBeTrue();
            actual.PadToDesiredResolution.ShouldBeTrue();
        }

        [Test]
        public void Should_ScaleToEvenDimensions_ForTransportStream()
        {
            FFmpegProfile ffmpegProfile = TestProfile() with
            {
                Resolution = new Resolution { Width = 1280, Height = 720 }
            };

            var version = new MediaVersion { Width = 706, Height = 362, SampleAspectRatio = "32:27" };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                version,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            IDisplaySize scaledSize = actual.ScaledSize.IfNone(new MediaVersion { Width = 0, Height = 0 });
            scaledSize.Width.ShouldBe(1280);
            scaledSize.Height.ShouldBe(554);
            actual.PadToDesiredResolution.ShouldBeTrue();
        }

        [Test]
        public void Should_ScaleBeyondMinSize_ForCrop_ForTransportStream()
        {
            FFmpegProfile ffmpegProfile = TestProfile() with
            {
                Resolution = new Resolution { Width = 1280, Height = 720 },
                ScalingBehavior = ScalingBehavior.Crop
            };

            var version = new MediaVersion { Width = 944, Height = 720, SampleAspectRatio = "1:1" };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                version,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            IDisplaySize scaledSize = actual.ScaledSize.IfNone(new MediaVersion { Width = 0, Height = 0 });
            scaledSize.Width.ShouldBe(1280);
            scaledSize.Height.ShouldBe(976);
            actual.PadToDesiredResolution.ShouldBeFalse();
        }

        [Test]
        public void Should_ScaleBeyondMinSize_ForCrop_ForTransportStream_UnknownSAR()
        {
            FFmpegProfile ffmpegProfile = TestProfile() with
            {
                Resolution = new Resolution { Width = 640, Height = 411 },
                ScalingBehavior = ScalingBehavior.Crop
            };

            var version = new MediaVersion
                { Width = 626, Height = 476, SampleAspectRatio = "0:0", DisplayAspectRatio = "4:3" };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                version,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            IDisplaySize scaledSize = actual.ScaledSize.IfNone(new MediaVersion { Width = 0, Height = 0 });
            scaledSize.Width.ShouldBe(640);
            scaledSize.Height.ShouldBe(480);
            actual.PadToDesiredResolution.ShouldBeFalse();
        }

        [Test]
        public void Should_ScaleDownToMinSize_ForCrop_ForTransportStream()
        {
            FFmpegProfile ffmpegProfile = TestProfile() with
            {
                Resolution = new Resolution { Width = 1280, Height = 720 },
                ScalingBehavior = ScalingBehavior.Crop
            };

            var version = new MediaVersion { Width = 1920, Height = 816, SampleAspectRatio = "1:1" };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                version,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            IDisplaySize scaledSize = actual.ScaledSize.IfNone(new MediaVersion { Width = 0, Height = 0 });
            scaledSize.Width.ShouldBe(1694);
            scaledSize.Height.ShouldBe(720);
            actual.PadToDesiredResolution.ShouldBeFalse();
        }

        [Test]
        public void Should_NotPadToDesiredResolution_When_UnscaledContentIsUnderSized_ForHttpLiveStreaming()
        {
            FFmpegProfile ffmpegProfile = TestProfile() with
            {
                Resolution = new Resolution { Width = 1920, Height = 1080 }
            };

            // not anamorphic
            var version = new MediaVersion { Width = 1918, Height = 1080, SampleAspectRatio = "1:1" };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.HttpLiveStreamingDirect,
                ffmpegProfile,
                version,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.ScaledSize.IsNone.ShouldBeTrue();
            actual.PadToDesiredResolution.ShouldBeFalse();
        }


        [Test]
        public void Should_SetDesiredVideoFormat_When_ContentIsPadded_ForTransportStream()
        {
            var ffmpegProfile = new FFmpegProfile
            {
                Resolution = new Resolution { Width = 1920, Height = 1080 },
                VideoFormat = FFmpegProfileVideoFormat.H264
            };

            // not anamorphic
            var version = new MediaVersion { Width = 1918, Height = 1080, SampleAspectRatio = "1:1" };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                version,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.ScaledSize.IsNone.ShouldBeTrue();
            actual.PadToDesiredResolution.ShouldBeTrue();
            actual.VideoFormat.ShouldBe(FFmpegProfileVideoFormat.H264);
        }

        [Test]
        public void
            Should_SetDesiredVideoFormat_When_ContentIsCorrectSize_ForTransportStream()
        {
            var ffmpegProfile = new FFmpegProfile
            {
                Resolution = new Resolution { Width = 1920, Height = 1080 },
                VideoFormat = FFmpegProfileVideoFormat.H264
            };

            // not anamorphic
            var version = new MediaVersion
                { Width = 1920, Height = 1080, SampleAspectRatio = "1:1" };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                version,
                new MediaStream { Codec = "mpeg2video" },
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.ScaledSize.IsNone.ShouldBeTrue();
            actual.PadToDesiredResolution.ShouldBeFalse();
            actual.VideoFormat.ShouldBe(FFmpegProfileVideoFormat.H264);
        }

        [Test]
        public void Should_SetCopyVideoFormat_When_ContentIsCorrectSize_ForHttpLiveStreamingDirect()
        {
            var ffmpegProfile = new FFmpegProfile
            {
                Resolution = new Resolution { Width = 1920, Height = 1080 },
                VideoFormat = FFmpegProfileVideoFormat.H264
            };

            // not anamorphic
            var version = new MediaVersion
                { Width = 1920, Height = 1080, SampleAspectRatio = "1:1" };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.HttpLiveStreamingDirect,
                ffmpegProfile,
                version,
                new MediaStream { Codec = "mpeg2video" },
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.ScaledSize.IsNone.ShouldBeTrue();
            actual.PadToDesiredResolution.ShouldBeFalse();
            actual.VideoFormat.ShouldBe(FFmpegProfileVideoFormat.Copy);
        }

        [Test]
        public void Should_NotSetCopyVideoFormat_When_ContentIsCorrectSize_And_CorrectFormat_ForTransportStream()
        {
            var ffmpegProfile = new FFmpegProfile
            {
                Resolution = new Resolution { Width = 1920, Height = 1080 },
                VideoFormat = FFmpegProfileVideoFormat.H264
            };

            // not anamorphic
            var version = new MediaVersion
                { Width = 1920, Height = 1080, SampleAspectRatio = "1:1" };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                version,
                new MediaStream { Codec = "h264" },
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.ScaledSize.IsNone.ShouldBeTrue();
            actual.PadToDesiredResolution.ShouldBeFalse();
            actual.VideoFormat.ShouldBe(FFmpegProfileVideoFormat.H264);
        }

        [Test]
        public void Should_SetVideoBitrate_When_ContentIsPadded_ForTransportStream()
        {
            var ffmpegProfile = new FFmpegProfile
            {
                Resolution = new Resolution { Width = 1920, Height = 1080 },
                VideoBitrate = 2525
            };

            // not anamorphic
            var version = new MediaVersion { Width = 1918, Height = 1080, SampleAspectRatio = "1:1" };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                version,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.ScaledSize.IsNone.ShouldBeTrue();
            actual.PadToDesiredResolution.ShouldBeTrue();
            actual.VideoBitrate.IfNone(0).ShouldBe(2525);
        }

        [Test]
        public void Should_SetVideoBitrate_When_ContentIsCorrectSize_ForTransportStream()
        {
            var ffmpegProfile = new FFmpegProfile
            {
                Resolution = new Resolution { Width = 1920, Height = 1080 },
                VideoBitrate = 2525
            };

            // not anamorphic
            var version = new MediaVersion
                { Width = 1920, Height = 1080, SampleAspectRatio = "1:1" };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                version,
                new MediaStream { Codec = "mpeg2video" },
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.ScaledSize.IsNone.ShouldBeTrue();
            actual.PadToDesiredResolution.ShouldBeFalse();
            actual.VideoBitrate.IfNone(0).ShouldBe(2525);
        }

        [Test]
        public void Should_SetVideoBufferSize_When_ContentIsPadded_ForTransportStream()
        {
            var ffmpegProfile = new FFmpegProfile
            {
                Resolution = new Resolution { Width = 1920, Height = 1080 },
                VideoBufferSize = 2525
            };

            // not anamorphic
            var version = new MediaVersion { Width = 1918, Height = 1080, SampleAspectRatio = "1:1" };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                version,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.ScaledSize.IsNone.ShouldBeTrue();
            actual.PadToDesiredResolution.ShouldBeTrue();
            actual.VideoBufferSize.IfNone(0).ShouldBe(2525);
        }

        [Test]
        public void Should_SetVideoBufferSize_When_ContentIsCorrectSize_ForTransportStream()
        {
            var ffmpegProfile = new FFmpegProfile
            {
                Resolution = new Resolution { Width = 1920, Height = 1080 },
                VideoBufferSize = 2525
            };

            // not anamorphic
            var version = new MediaVersion
                { Width = 1920, Height = 1080, SampleAspectRatio = "1:1" };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                version,
                new MediaStream { Codec = "mpeg2video" },
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.ScaledSize.IsNone.ShouldBeTrue();
            actual.PadToDesiredResolution.ShouldBeFalse();
            actual.VideoBufferSize.IfNone(0).ShouldBe(2525);
        }

        [Test]
        public void Should_SetDesiredAudioFormat_With_CorrectFormat_ForTransportStream()
        {
            FFmpegProfile ffmpegProfile = TestProfile() with
            {
                AudioFormat = FFmpegProfileAudioFormat.Aac
            };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                TestVersion,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.AudioFormat.ShouldBe(FFmpegProfileAudioFormat.Aac);
        }

        [Test]
        public void Should_SetDesiredAudioFormat_ForTransportStream()
        {
            FFmpegProfile ffmpegProfile = TestProfile() with
            {
                AudioFormat = FFmpegProfileAudioFormat.Aac
            };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                TestVersion,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.AudioFormat.ShouldBe(FFmpegProfileAudioFormat.Aac);
        }

        [Test]
        public void Should_SetCopyAudioFormat_ForHttpLiveStreamingDirect()
        {
            FFmpegProfile ffmpegProfile = TestProfile() with
            {
                AudioFormat = FFmpegProfileAudioFormat.Aac
            };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.HttpLiveStreamingDirect,
                ffmpegProfile,
                TestVersion,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.AudioFormat.ShouldBe(FFmpegProfileAudioFormat.Copy);
        }

        [Test]
        public void Should_SetAudioBitrate_With_CorrectFormat_ForTransportStream()
        {
            FFmpegProfile ffmpegProfile = TestProfile() with
            {
                AudioBitrate = 2424,
                AudioFormat = FFmpegProfileAudioFormat.Ac3
            };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                TestVersion,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.AudioBitrate.IfNone(0).ShouldBe(2424);
        }

        [Test]
        public void Should_SetAudioBufferSize_With_CorrectFormat_ForTransportStream()
        {
            FFmpegProfile ffmpegProfile = TestProfile() with
            {
                AudioBufferSize = 2424,
                AudioFormat = FFmpegProfileAudioFormat.Ac3
            };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                TestVersion,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.AudioBufferSize.IfNone(0).ShouldBe(2424);
        }

        [Test]
        public void Should_SetAudioChannels_With_CorrectFormat_ForTransportStream()
        {
            FFmpegProfile ffmpegProfile = TestProfile() with
            {
                AudioFormat = FFmpegProfileAudioFormat.Ac3,
                AudioChannels = 6
            };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                TestVersion,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.AudioChannels.IfNone(0).ShouldBe(6);
        }

        [Test]
        public void Should_SetAudioSampleRate_With_CorrectFormat_ForTransportStream()
        {
            FFmpegProfile ffmpegProfile = TestProfile() with
            {
                AudioFormat = FFmpegProfileAudioFormat.Ac3,
                AudioSampleRate = 48
            };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                TestVersion,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.AudioSampleRate.IfNone(0).ShouldBe(48);
        }

        [Test]
        public void Should_SetAudioChannels_ForTransportStream()
        {
            FFmpegProfile ffmpegProfile = TestProfile() with
            {
                AudioChannels = 6
            };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                TestVersion,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.AudioChannels.IfNone(0).ShouldBe(6);
        }

        [Test]
        public void Should_SetAudioSampleRate_ForTransportStream()
        {
            FFmpegProfile ffmpegProfile = TestProfile() with
            {
                AudioSampleRate = 48
            };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                TestVersion,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.AudioSampleRate.IfNone(0).ShouldBe(48);
        }

        [Test]
        public void Should_SetPadAudio_ForTransportStream()
        {
            FFmpegProfile ffmpegProfile = TestProfile() with
            {
                AudioSampleRate = 48,
                AudioFormat = FFmpegProfileAudioFormat.Ac3
            };

            var version = new MediaVersion
            {
                SampleAspectRatio = "1:1", Width = 1920, Height = 1080, Duration = TimeSpan.FromMinutes(5)
            }; // not pulled from here

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                version,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.PadAudio.ShouldBe(true);
        }

        [Test]
        public void Should_SetNormalizeLoudness_ForTransportStream()
        {
            FFmpegProfile ffmpegProfile = TestProfile() with
            {
                NormalizeLoudnessMode = NormalizeLoudnessMode.LoudNorm
            };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                TestVersion,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.NormalizeLoudnessMode.ShouldBe(NormalizeLoudnessMode.LoudNorm);
        }
    }

    [TestFixture]
    public class CalculateSettingsQsv
    {
        [Test]
        public void Should_UseHardwareAcceleration()
        {
            FFmpegProfile ffmpegProfile =
                TestProfile() with { HardwareAcceleration = HardwareAccelerationKind.Qsv };

            FFmpegPlaybackSettings actual = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                ffmpegProfile,
                TestVersion,
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                None);

            actual.HardwareAcceleration.ShouldBe(HardwareAccelerationKind.Qsv);
        }
    }

    private static FFmpegProfile TestProfile() =>
        new() { Resolution = new Resolution { Width = 1920, Height = 1080 } };
}
