using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using FluentAssertions;
using NUnit.Framework;

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
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.FormatFlags.Should().NotContain("+genpts");
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
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.ThreadCount.Should().Be(7);
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
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.ThreadCount.Should().Be(7);
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
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            string[] expected = { "+genpts", "+discardcorrupt", "+igndts" };
            actual.FormatFlags.Count.Should().Be(expected.Length);
            actual.FormatFlags.Should().Contain(expected);
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
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            string[] expected = { "+genpts", "+discardcorrupt", "+igndts" };
            actual.FormatFlags.Count.Should().Be(expected.Length);
            actual.FormatFlags.Should().Contain(expected);
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
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.RealtimeOutput.Should().BeTrue();
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
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.RealtimeOutput.Should().BeTrue();
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
                new MediaStream(),
                now,
                now.AddMinutes(5),
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.StreamSeek.IsSome.Should().BeTrue();
            actual.StreamSeek.IfNone(TimeSpan.Zero).Should().Be(TimeSpan.FromMinutes(5));
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
                new MediaStream(),
                now,
                now.AddMinutes(5),
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.StreamSeek.IsSome.Should().BeTrue();
            actual.StreamSeek.IfNone(TimeSpan.Zero).Should().Be(TimeSpan.FromMinutes(5));
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
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.ScaledSize.IsNone.Should().BeTrue();
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
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.ScaledSize.IsNone.Should().BeTrue();
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
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.ScaledSize.IsNone.Should().BeTrue();
            actual.PadToDesiredResolution.Should().BeFalse();
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
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.ScaledSize.IsNone.Should().BeTrue();
            actual.PadToDesiredResolution.Should().BeTrue();
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
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            IDisplaySize scaledSize = actual.ScaledSize.IfNone(new MediaVersion { Width = 0, Height = 0 });
            scaledSize.Width.Should().Be(1280);
            scaledSize.Height.Should().Be(554);
            actual.PadToDesiredResolution.Should().BeTrue();
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
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.ScaledSize.IsNone.Should().BeTrue();
            actual.PadToDesiredResolution.Should().BeFalse();
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
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.ScaledSize.IsNone.Should().BeTrue();
            actual.PadToDesiredResolution.Should().BeTrue();
            actual.VideoFormat.Should().Be(FFmpegProfileVideoFormat.H264);
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
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.ScaledSize.IsNone.Should().BeTrue();
            actual.PadToDesiredResolution.Should().BeFalse();
            actual.VideoFormat.Should().Be(FFmpegProfileVideoFormat.H264);
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
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.ScaledSize.IsNone.Should().BeTrue();
            actual.PadToDesiredResolution.Should().BeFalse();
            actual.VideoFormat.Should().Be(FFmpegProfileVideoFormat.Copy);
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
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.ScaledSize.IsNone.Should().BeTrue();
            actual.PadToDesiredResolution.Should().BeFalse();
            actual.VideoFormat.Should().Be(FFmpegProfileVideoFormat.H264);
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
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.ScaledSize.IsNone.Should().BeTrue();
            actual.PadToDesiredResolution.Should().BeTrue();
            actual.VideoBitrate.IfNone(0).Should().Be(2525);
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
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.ScaledSize.IsNone.Should().BeTrue();
            actual.PadToDesiredResolution.Should().BeFalse();
            actual.VideoBitrate.IfNone(0).Should().Be(2525);
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
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.ScaledSize.IsNone.Should().BeTrue();
            actual.PadToDesiredResolution.Should().BeTrue();
            actual.VideoBufferSize.IfNone(0).Should().Be(2525);
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
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.ScaledSize.IsNone.Should().BeTrue();
            actual.PadToDesiredResolution.Should().BeFalse();
            actual.VideoBufferSize.IfNone(0).Should().Be(2525);
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
                new MediaStream { Codec = "aac" },
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.AudioFormat.Should().Be(FFmpegProfileAudioFormat.Aac);
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
                new MediaStream { Codec = "ac3" },
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.AudioFormat.Should().Be(FFmpegProfileAudioFormat.Aac);
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
                new MediaStream { Codec = "ac3" },
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.AudioFormat.Should().Be(FFmpegProfileAudioFormat.Copy);
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
                new MediaStream { Codec = "ac3" },
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.AudioBitrate.IfNone(0).Should().Be(2424);
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
                new MediaStream { Codec = "ac3" },
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.AudioBufferSize.IfNone(0).Should().Be(2424);
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
                new MediaStream { Codec = "ac3" },
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.AudioChannels.IfNone(0).Should().Be(6);
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
                new MediaStream { Codec = "ac3" },
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.AudioSampleRate.IfNone(0).Should().Be(48);
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
                new MediaStream { Codec = "ac3" },
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.AudioChannels.IfNone(0).Should().Be(6);
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
                new MediaStream { Codec = "ac3" },
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.AudioSampleRate.IfNone(0).Should().Be(48);
        }

        [Test]
        public void Should_SetAudioDuration_With_CorrectFormat_ForTransportStream()
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
                new MediaStream { Codec = "ac3" },
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.FromMinutes(2),
                false,
                None);

            actual.AudioDuration.IfNone(TimeSpan.MinValue).Should().Be(TimeSpan.FromMinutes(2));
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
                new MediaStream { Codec = "ac3" },
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.NormalizeLoudnessMode.Should().Be(NormalizeLoudnessMode.LoudNorm);
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
                new MediaStream(),
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                None);

            actual.HardwareAcceleration.Should().Be(HardwareAccelerationKind.Qsv);
        }
    }

    private static FFmpegProfile TestProfile() =>
        new() { Resolution = new Resolution { Width = 1920, Height = 1080 } };
}
