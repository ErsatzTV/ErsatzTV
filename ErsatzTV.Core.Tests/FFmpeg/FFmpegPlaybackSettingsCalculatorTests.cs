using System;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using FluentAssertions;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.FFmpeg
{
    [TestFixture]
    public class FFmpegPlaybackSettingsCalculatorTests
    {
        [TestFixture]
        public class CalculateSettings
        {
            private readonly FFmpegPlaybackSettingsCalculator _calculator;

            public CalculateSettings() => _calculator = new FFmpegPlaybackSettingsCalculator();

            [Test]
            public void Should_UseSpecifiedThreadCount_ForTransportStream()
            {
                FFmpegProfile ffmpegProfile = TestProfile() with { ThreadCount = 7 };

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    new MediaVersion(),
                    new MediaStream(),
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.ThreadCount.Should().Be(7);
            }

            [Test]
            public void Should_UseSpecifiedThreadCount_ForHttpLiveStreaming()
            {
                FFmpegProfile ffmpegProfile = TestProfile() with { ThreadCount = 7 };

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.HttpLiveStreamingDirect,
                    ffmpegProfile,
                    new MediaVersion(),
                    new MediaStream(),
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.ThreadCount.Should().Be(7);
            }

            [Test]
            public void Should_SetFormatFlags_ForTransportStream()
            {
                FFmpegProfile ffmpegProfile = TestProfile();

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    new MediaVersion(),
                    new MediaStream(),
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                string[] expected = { "+genpts", "+discardcorrupt", "+igndts" };
                actual.FormatFlags.Count.Should().Be(expected.Length);
                actual.FormatFlags.Should().Contain(expected);
            }

            [Test]
            public void Should_SetFormatFlags_ForHttpLiveStreaming()
            {
                FFmpegProfile ffmpegProfile = TestProfile();

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.HttpLiveStreamingDirect,
                    ffmpegProfile,
                    new MediaVersion(),
                    new MediaStream(),
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                string[] expected = { "+genpts", "+discardcorrupt", "+igndts" };
                actual.FormatFlags.Count.Should().Be(expected.Length);
                actual.FormatFlags.Should().Contain(expected);
            }

            [Test]
            public void Should_SetRealtime_ForTransportStream()
            {
                FFmpegProfile ffmpegProfile = TestProfile();

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    new MediaVersion(),
                    new MediaStream(),
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.RealtimeOutput.Should().BeTrue();
            }

            [Test]
            public void Should_SetRealtime_ForHttpLiveStreaming()
            {
                FFmpegProfile ffmpegProfile = TestProfile();

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.HttpLiveStreamingDirect,
                    ffmpegProfile,
                    new MediaVersion(),
                    new MediaStream(),
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.RealtimeOutput.Should().BeTrue();
            }

            [Test]
            public void Should_SetStreamSeek_When_PlaybackIsLate_ForTransportStream()
            {
                DateTimeOffset now = DateTimeOffset.Now;

                FFmpegProfile ffmpegProfile = TestProfile();

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    new MediaVersion(),
                    new MediaStream(),
                    new MediaStream(),
                    now,
                    now.AddMinutes(5));

                actual.StreamSeek.IsSome.Should().BeTrue();
                actual.StreamSeek.IfNone(TimeSpan.Zero).Should().Be(TimeSpan.FromMinutes(5));
            }

            [Test]
            public void Should_SetStreamSeek_When_PlaybackIsLate_ForHttpLiveStreaming()
            {
                DateTimeOffset now = DateTimeOffset.Now;

                FFmpegProfile ffmpegProfile = TestProfile();

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.HttpLiveStreamingDirect,
                    ffmpegProfile,
                    new MediaVersion(),
                    new MediaStream(),
                    new MediaStream(),
                    now,
                    now.AddMinutes(5));

                actual.StreamSeek.IsSome.Should().BeTrue();
                actual.StreamSeek.IfNone(TimeSpan.Zero).Should().Be(TimeSpan.FromMinutes(5));
            }

            [Test]
            public void ShouldNot_SetScaledSize_When_NotNormalizingVideo_ForTransportStream()
            {
                FFmpegProfile ffmpegProfile = TestProfile() with { NormalizeVideo = false };

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    new MediaVersion(),
                    new MediaStream(),
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.ScaledSize.IsNone.Should().BeTrue();
            }

            [Test]
            public void ShouldNot_SetScaledSize_When_ContentIsCorrectSize_ForTransportStream()
            {
                FFmpegProfile ffmpegProfile = TestProfile() with
                {
                    NormalizeVideo = true,
                    Resolution = new Resolution { Width = 1920, Height = 1080 }
                };

                // not anamorphic
                var version = new MediaVersion { Width = 1920, Height = 1080, SampleAspectRatio = "1:1" };

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.ScaledSize.IsNone.Should().BeTrue();
            }

            [Test]
            public void ShouldNot_SetScaledSize_When_ScaledSizeWouldEqualContentSize_ForTransportStream()
            {
                FFmpegProfile ffmpegProfile = TestProfile() with
                {
                    NormalizeVideo = true,
                    Resolution = new Resolution { Width = 1920, Height = 1080 }
                };

                // not anamorphic
                var version = new MediaVersion { Width = 1918, Height = 1080, SampleAspectRatio = "1:1" };

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.ScaledSize.IsNone.Should().BeTrue();
            }

            [Test]
            public void ShouldNot_PadToDesiredResolution_When_ContentIsCorrectSize_ForTransportStream()
            {
                FFmpegProfile ffmpegProfile = TestProfile() with
                {
                    NormalizeVideo = true,
                    Resolution = new Resolution { Width = 1920, Height = 1080 }
                };

                // not anamorphic
                var version = new MediaVersion { Width = 1920, Height = 1080, SampleAspectRatio = "1:1" };

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.ScaledSize.IsNone.Should().BeTrue();
                actual.PadToDesiredResolution.Should().BeFalse();
            }

            [Test]
            public void Should_PadToDesiredResolution_When_UnscaledContentIsUnderSized_ForTransportStream()
            {
                FFmpegProfile ffmpegProfile = TestProfile() with
                {
                    Transcode = true,
                    NormalizeVideo = true,
                    Resolution = new Resolution { Width = 1920, Height = 1080 }
                };

                // not anamorphic
                var version = new MediaVersion { Width = 1918, Height = 1080, SampleAspectRatio = "1:1" };

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.ScaledSize.IsNone.Should().BeTrue();
                actual.PadToDesiredResolution.Should().BeTrue();
            }

            [Test]
            public void Should_ScaleToEvenDimensions_ForTransportStream()
            {
                FFmpegProfile ffmpegProfile = TestProfile() with
                {
                    Transcode = true,
                    NormalizeVideo = true,
                    Resolution = new Resolution { Width = 1280, Height = 720 }
                };

                var version = new MediaVersion { Width = 706, Height = 362, SampleAspectRatio = "32:27" };

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

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
                    Transcode = true,
                    NormalizeVideo = true,
                    Resolution = new Resolution { Width = 1920, Height = 1080 }
                };

                // not anamorphic
                var version = new MediaVersion { Width = 1918, Height = 1080, SampleAspectRatio = "1:1" };

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.HttpLiveStreamingDirect,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.ScaledSize.IsNone.Should().BeTrue();
                actual.PadToDesiredResolution.Should().BeFalse();
            }

            [Test]
            public void Should_NotPadToDesiredResolution_When_NotNormalizingVideo()
            {
                FFmpegProfile ffmpegProfile = TestProfile() with
                {
                    Transcode = true,
                    NormalizeVideo = false,
                    Resolution = new Resolution { Width = 1920, Height = 1080 }
                };

                // not anamorphic
                var version = new MediaVersion { Width = 1918, Height = 1080, SampleAspectRatio = "1:1" };

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.ScaledSize.IsNone.Should().BeTrue();
                actual.PadToDesiredResolution.Should().BeFalse();
            }

            [Test]
            public void Should_SetDesiredVideoCodec_When_ContentIsPadded_ForTransportStream()
            {
                var ffmpegProfile = new FFmpegProfile
                {
                    Transcode = true,
                    NormalizeVideo = true,
                    Resolution = new Resolution { Width = 1920, Height = 1080 },
                    VideoCodec = "testCodec"
                };

                // not anamorphic
                var version = new MediaVersion { Width = 1918, Height = 1080, SampleAspectRatio = "1:1" };

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.ScaledSize.IsNone.Should().BeTrue();
                actual.PadToDesiredResolution.Should().BeTrue();
                actual.VideoCodec.Should().Be("testCodec");
            }

            [Test]
            public void
                Should_SetDesiredVideoCodec_When_ContentIsCorrectSize_And_NormalizingVideo_ForTransportStream()
            {
                var ffmpegProfile = new FFmpegProfile
                {
                    Transcode = true,
                    NormalizeVideo = true,
                    Resolution = new Resolution { Width = 1920, Height = 1080 },
                    VideoCodec = "testCodec"
                };

                // not anamorphic
                var version = new MediaVersion
                    { Width = 1920, Height = 1080, SampleAspectRatio = "1:1" };

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream { Codec = "mpeg2video" },
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.ScaledSize.IsNone.Should().BeTrue();
                actual.PadToDesiredResolution.Should().BeFalse();
                actual.VideoCodec.Should().Be("testCodec");
            }

            [Test]
            public void
                Should_SetCopyVideoCodec_When_ContentIsCorrectSize_And_NormalizingVideo_ForHttpLiveStreaming()
            {
                var ffmpegProfile = new FFmpegProfile
                {
                    Transcode = true,
                    NormalizeVideo = true,
                    Resolution = new Resolution { Width = 1920, Height = 1080 },
                    VideoCodec = "testCodec"
                };

                // not anamorphic
                var version = new MediaVersion
                    { Width = 1920, Height = 1080, SampleAspectRatio = "1:1" };

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.HttpLiveStreamingDirect,
                    ffmpegProfile,
                    version,
                    new MediaStream { Codec = "mpeg2video" },
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.ScaledSize.IsNone.Should().BeTrue();
                actual.PadToDesiredResolution.Should().BeFalse();
                actual.VideoCodec.Should().Be("copy");
            }

            [Test]
            public void Should_SetCopyVideoCodec_When_ContentIsCorrectSize_And_CorrectCodec_ForTransportStream()
            {
                var ffmpegProfile = new FFmpegProfile
                {
                    Transcode = true,
                    NormalizeVideo = true,
                    Resolution = new Resolution { Width = 1920, Height = 1080 },
                    VideoCodec = "libx264"
                };

                // not anamorphic
                var version = new MediaVersion
                    { Width = 1920, Height = 1080, SampleAspectRatio = "1:1" };

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream { Codec = "libx264" },
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.ScaledSize.IsNone.Should().BeTrue();
                actual.PadToDesiredResolution.Should().BeFalse();
                actual.VideoCodec.Should().Be("copy");
            }

            [Test]
            public void
                Should_SetCopyVideoCodec_When_ContentIsCorrectSize_And_NotNormalizingVideo_ForTransportStream()
            {
                var ffmpegProfile = new FFmpegProfile
                {
                    Transcode = true,
                    NormalizeVideo = false,
                    Resolution = new Resolution { Width = 1920, Height = 1080 },
                    VideoCodec = "libx264"
                };

                // not anamorphic
                var version = new MediaVersion
                    { Width = 1920, Height = 1080, SampleAspectRatio = "1:1" };

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream { Codec = "mpeg2video" },
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.ScaledSize.IsNone.Should().BeTrue();
                actual.PadToDesiredResolution.Should().BeFalse();
                actual.VideoCodec.Should().Be("copy");
            }
            
            [Test]
            public void
                Should_SetCopyVideoCodec_AndCopyAudioCodec_When_NotTranscoding_ForTransportStream()
            {
                var ffmpegProfile = new FFmpegProfile
                {
                    Transcode = false,
                    NormalizeVideo = true,
                    NormalizeAudio = true,
                    NormalizeLoudness = true,
                    Resolution = new Resolution { Width = 1920, Height = 1080 },
                    VideoCodec = "libx264"
                };

                // not anamorphic
                var version = new MediaVersion
                    { Width = 1920, Height = 1080, SampleAspectRatio = "1:1" };

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream { Codec = "mpeg2video" },
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.ScaledSize.IsNone.Should().BeTrue();
                actual.PadToDesiredResolution.Should().BeFalse();
                actual.VideoCodec.Should().Be("copy");
                actual.NormalizeLoudness.Should().BeFalse();
                actual.AudioCodec.Should().Be("copy");
            }

            [Test]
            public void Should_SetVideoBitrate_When_ContentIsPadded_ForTransportStream()
            {
                var ffmpegProfile = new FFmpegProfile
                {
                    Transcode = true,
                    NormalizeVideo = true,
                    Resolution = new Resolution { Width = 1920, Height = 1080 },
                    VideoBitrate = 2525
                };

                // not anamorphic
                var version = new MediaVersion { Width = 1918, Height = 1080, SampleAspectRatio = "1:1" };

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.ScaledSize.IsNone.Should().BeTrue();
                actual.PadToDesiredResolution.Should().BeTrue();
                actual.VideoBitrate.IfNone(0).Should().Be(2525);
            }

            [Test]
            public void Should_SetVideoBitrate_When_ContentIsCorrectSize_And_NormalizingVideo_ForTransportStream()
            {
                var ffmpegProfile = new FFmpegProfile
                {
                    Transcode = true,
                    NormalizeVideo = true,
                    Resolution = new Resolution { Width = 1920, Height = 1080 },
                    VideoBitrate = 2525
                };

                // not anamorphic
                var version = new MediaVersion
                    { Width = 1920, Height = 1080, SampleAspectRatio = "1:1" };

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream { Codec = "mpeg2video" },
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.ScaledSize.IsNone.Should().BeTrue();
                actual.PadToDesiredResolution.Should().BeFalse();
                actual.VideoBitrate.IfNone(0).Should().Be(2525);
            }

            [Test]
            public void Should_SetVideoBufferSize_When_ContentIsPadded_ForTransportStream()
            {
                var ffmpegProfile = new FFmpegProfile
                {
                    Transcode = true,
                    NormalizeVideo = true,
                    Resolution = new Resolution { Width = 1920, Height = 1080 },
                    VideoBufferSize = 2525
                };

                // not anamorphic
                var version = new MediaVersion { Width = 1918, Height = 1080, SampleAspectRatio = "1:1" };

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.ScaledSize.IsNone.Should().BeTrue();
                actual.PadToDesiredResolution.Should().BeTrue();
                actual.VideoBufferSize.IfNone(0).Should().Be(2525);
            }

            [Test]
            public void
                Should_SetVideoBufferSize_When_ContentIsCorrectSize_And_NormalizingVideo_ForTransportStream()
            {
                var ffmpegProfile = new FFmpegProfile
                {
                    Transcode = true,
                    NormalizeVideo = true,
                    Resolution = new Resolution { Width = 1920, Height = 1080 },
                    VideoBufferSize = 2525
                };

                // not anamorphic
                var version = new MediaVersion
                    { Width = 1920, Height = 1080, SampleAspectRatio = "1:1" };

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream { Codec = "mpeg2video" },
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.ScaledSize.IsNone.Should().BeTrue();
                actual.PadToDesiredResolution.Should().BeFalse();
                actual.VideoBufferSize.IfNone(0).Should().Be(2525);
            }

            [Test]
            public void Should_SetDesiredAudioCodec_When_NormalizingAudio_With_CorrectCodec_ForTransportStream()
            {
                FFmpegProfile ffmpegProfile = TestProfile() with
                {
                    Transcode = true,
                    NormalizeAudio = true,
                    AudioCodec = "aac"
                };

                var version = new MediaVersion();

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream { Codec = "aac" },
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.AudioCodec.Should().Be("aac");
            }

            [Test]
            public void Should_SetCopyAudioCodec_When_NotNormalizingAudio_ForTransportStream()
            {
                FFmpegProfile ffmpegProfile = TestProfile() with
                {
                    NormalizeAudio = false,
                    AudioCodec = "aac"
                };

                var version = new MediaVersion();

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream { Codec = "ac3" },
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.AudioCodec.Should().Be("copy");
            }

            [Test]
            public void Should_SetDesiredAudioCodec_When_NormalizingAudio_ForTransportStream()
            {
                FFmpegProfile ffmpegProfile = TestProfile() with
                {
                    Transcode = true,
                    NormalizeAudio = true,
                    AudioCodec = "aac"
                };

                var version = new MediaVersion();

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream { Codec = "ac3" },
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.AudioCodec.Should().Be("aac");
            }

            [Test]
            public void Should_SetCopyAudioCodec_When_NormalizingAudio_ForHttpLiveStreaming()
            {
                FFmpegProfile ffmpegProfile = TestProfile() with
                {
                    Transcode = true,
                    NormalizeAudio = true,
                    AudioCodec = "aac"
                };

                var version = new MediaVersion();

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.HttpLiveStreamingDirect,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream { Codec = "ac3" },
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.AudioCodec.Should().Be("copy");
            }

            [Test]
            public void Should_SetAudioBitrate_When_NormalizingAudio_With_CorrectCodec_ForTransportStream()
            {
                FFmpegProfile ffmpegProfile = TestProfile() with
                {
                    Transcode = true,
                    NormalizeAudio = true,
                    AudioBitrate = 2424,
                    AudioCodec = "ac3"
                };

                var version = new MediaVersion();

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream { Codec = "ac3" },
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.AudioBitrate.IfNone(0).Should().Be(2424);
            }

            [Test]
            public void Should_SetAudioBufferSize_When_NormalizingAudio_With_CorrectCodec_ForTransportStream()
            {
                FFmpegProfile ffmpegProfile = TestProfile() with
                {
                    Transcode = true,
                    NormalizeAudio = true,
                    AudioBufferSize = 2424,
                    AudioCodec = "ac3"
                };

                var version = new MediaVersion();

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream { Codec = "ac3" },
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.AudioBufferSize.IfNone(0).Should().Be(2424);
            }

            [Test]
            public void Should_SetAudioChannels_When_NormalizingAudio_With_CorrectCodec_ForTransportStream()
            {
                FFmpegProfile ffmpegProfile = TestProfile() with
                {
                    Transcode = true,
                    NormalizeAudio = true,
                    AudioCodec = "ac3",
                    AudioChannels = 6
                };

                var version = new MediaVersion();

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream { Codec = "ac3" },
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.AudioChannels.IfNone(0).Should().Be(6);
            }

            [Test]
            public void Should_SetAudioSampleRate_When_NormalizingAudio_With_CorrectCodec_ForTransportStream()
            {
                FFmpegProfile ffmpegProfile = TestProfile() with
                {
                    Transcode = true,
                    NormalizeAudio = true,
                    AudioCodec = "ac3",
                    AudioSampleRate = 48
                };

                var version = new MediaVersion();

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream { Codec = "ac3" },
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.AudioSampleRate.IfNone(0).Should().Be(48);
            }

            [Test]
            public void Should_SetAudioChannels_When_NormalizingAudio_ForTransportStream()
            {
                FFmpegProfile ffmpegProfile = TestProfile() with
                {
                    Transcode = true,
                    NormalizeAudio = true,
                    AudioChannels = 6
                };

                var version = new MediaVersion();

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream { Codec = "ac3" },
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.AudioChannels.IfNone(0).Should().Be(6);
            }

            [Test]
            public void Should_SetAudioSampleRate_When_NormalizingAudio_ForTransportStream()
            {
                FFmpegProfile ffmpegProfile = TestProfile() with
                {
                    Transcode = true,
                    NormalizeAudio = true,
                    AudioSampleRate = 48
                };

                var version = new MediaVersion();

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream { Codec = "ac3" },
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.AudioSampleRate.IfNone(0).Should().Be(48);
            }

            [Test]
            public void Should_SetAudioDuration_When_NormalizingAudio_With_CorrectCodec_ForTransportStream()
            {
                FFmpegProfile ffmpegProfile = TestProfile() with
                {
                    Transcode = true,
                    NormalizeAudio = true,
                    AudioSampleRate = 48,
                    AudioCodec = "ac3"
                };

                var version = new MediaVersion { Duration = TimeSpan.FromMinutes(2) };

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream { Codec = "ac3" },
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.AudioDuration.IfNone(TimeSpan.MinValue).Should().Be(TimeSpan.FromMinutes(2));
            }

            [Test]
            public void Should_SetNormalizeLoudness_When_NormalizingAudio_ForTransportStream()
            {
                FFmpegProfile ffmpegProfile = TestProfile() with
                {
                    Transcode = true,
                    NormalizeAudio = true,
                    NormalizeLoudness = true
                };

                var version = new MediaVersion();

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream { Codec = "ac3" },
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.NormalizeLoudness.Should().BeTrue();
            }

            [Test]
            public void Should_NotSetNormalizeLoudness_When_NotNormalizingAudio_ForTransportStream()
            {
                FFmpegProfile ffmpegProfile = TestProfile() with
                {
                    Transcode = true,
                    NormalizeAudio = false,
                    NormalizeLoudness = true
                };

                var version = new MediaVersion();

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    version,
                    new MediaStream(),
                    new MediaStream { Codec = "ac3" },
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.NormalizeLoudness.Should().BeFalse();
            }
        }

        [TestFixture]
        public class CalculateSettingsQsv
        {
            private readonly FFmpegPlaybackSettingsCalculator _calculator;

            public CalculateSettingsQsv() => _calculator = new FFmpegPlaybackSettingsCalculator();

            [Test]
            public void Should_UseHardwareAcceleration()
            {
                FFmpegProfile ffmpegProfile =
                    TestProfile() with { HardwareAcceleration = HardwareAccelerationKind.Qsv };

                FFmpegPlaybackSettings actual = _calculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    ffmpegProfile,
                    new MediaVersion(),
                    new MediaStream(),
                    new MediaStream(),
                    DateTimeOffset.Now,
                    DateTimeOffset.Now);

                actual.HardwareAcceleration.Should().Be(HardwareAccelerationKind.Qsv);
            }
        }

        private static FFmpegProfile TestProfile() =>
            new() { Resolution = new Resolution { Width = 1920, Height = 1080 } };
    }
}
