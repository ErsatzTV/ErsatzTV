using System.Collections.Immutable;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Scripting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests.FFmpeg;

[TestFixture]
public class FFmpegStreamSelectorTests
{
    [TestFixture]
    public class SelectAudioStream
    {
        [Test]
        [CancelAfter(1000)]
        public async Task Should_Select_Audio_Stream_With_Preferred_Language(CancellationToken cancellationToken)
        {
            // skip movie/episode script paths by using other video
            var mediaItem = new OtherVideo();
            var mediaVersion = new MediaVersion
            {
                Streams =
                [
                    new MediaStream
                    {
                        Index = 0,
                        MediaStreamKind = MediaStreamKind.Audio,
                        Channels = 2,
                        Language = "ja",
                        Title = "Some Title"
                    },
                    new MediaStream
                    {
                        Index = 1,
                        MediaStreamKind = MediaStreamKind.Audio,
                        Channels = 6,
                        Language = "eng",
                        Title = "Another Title",
                        Default = true
                    }
                ]
            };

            var audioVersion = new MediaItemAudioVersion(mediaItem, mediaVersion);
            var channel = new Channel(Guid.NewGuid())
            {
                PreferredAudioLanguageCode = "eng"
            };

            ILanguageCodeService languageCodeService = Substitute.For<ILanguageCodeService>();
            languageCodeService.GetAllLanguageCodes(Arg.Any<List<string>>())
                .Returns(["jpn"]);

            var selector = new FFmpegStreamSelector(
                new ScriptEngine(Substitute.For<ILogger<ScriptEngine>>()),
                Substitute.For<IStreamSelectorRepository>(),
                Substitute.For<IConfigElementRepository>(),
                Substitute.For<ILocalFileSystem>(),
                languageCodeService,
                Substitute.For<ILogger<FFmpegStreamSelector>>());

            Option<MediaStream> selectedStream = await selector.SelectAudioStream(
                audioVersion,
                StreamingMode.TransportStream,
                channel,
                "jpn",
                "Whatever",
                cancellationToken);
            selectedStream.IsSome.ShouldBeTrue();
            foreach (MediaStream stream in selectedStream)
            {
                stream.Language.ShouldBe("ja");
            }
        }

        [Test]
        [CancelAfter(1000)]
        public async Task Should_Select_Audio_Stream_With_Preferred_Title(CancellationToken cancellationToken)
        {
            // skip movie/episode script paths by using other video
            var mediaItem = new OtherVideo();
            var mediaVersion = new MediaVersion
            {
                Streams =
                [
                    new MediaStream
                    {
                        Index = 0,
                        MediaStreamKind = MediaStreamKind.Audio,
                        Channels = 2,
                        Language = "ja",
                        Title = "Some Title"
                    },
                    new MediaStream
                    {
                        Index = 1,
                        MediaStreamKind = MediaStreamKind.Audio,
                        Channels = 6,
                        Language = "eng",
                        Title = "Another Title",
                        Default = true
                    }
                ]
            };

            var audioVersion = new MediaItemAudioVersion(mediaItem, mediaVersion);
            var channel = new Channel(Guid.NewGuid())
            {
                PreferredAudioTitle = "Some"
            };

            ILanguageCodeService languageCodeService = Substitute.For<ILanguageCodeService>();
            languageCodeService.GetAllLanguageCodes(Arg.Any<List<string>>())
                .Returns(["jpn", "eng"]);

            var selector = new FFmpegStreamSelector(
                new ScriptEngine(Substitute.For<ILogger<ScriptEngine>>()),
                Substitute.For<IStreamSelectorRepository>(),
                Substitute.For<IConfigElementRepository>(),
                Substitute.For<ILocalFileSystem>(),
                languageCodeService,
                Substitute.For<ILogger<FFmpegStreamSelector>>());

            Option<MediaStream> selectedStream = await selector.SelectAudioStream(
                audioVersion,
                StreamingMode.TransportStream,
                channel,
                null,
                channel.PreferredAudioTitle,
                cancellationToken);
            selectedStream.IsSome.ShouldBeTrue();
            foreach (MediaStream stream in selectedStream)
            {
                stream.Language.ShouldBe("ja");
            }
        }

        [Test]
        [CancelAfter(1000)]
        public async Task Should_Select_Subtitle_Stream_With_Preferred_Language(CancellationToken cancellationToken)
        {
            // skip movie/episode script paths by using other video
            var subtitles = new List<Subtitle>
            {
                new()
                {
                    StreamIndex = 0,
                    SubtitleKind = SubtitleKind.Sidecar,
                    Language = "eng",
                    Default = true
                },
                new()
                {
                    StreamIndex = 1,
                    SubtitleKind = SubtitleKind.Sidecar,
                    Language = "he"
                }
            };

            var channel = new Channel(Guid.NewGuid());

            ILanguageCodeService languageCodeService = Substitute.For<ILanguageCodeService>();
            languageCodeService.GetAllLanguageCodes(Arg.Any<List<string>>())
                .Returns(["heb"]);

            var selector = new FFmpegStreamSelector(
                new ScriptEngine(Substitute.For<ILogger<ScriptEngine>>()),
                Substitute.For<IStreamSelectorRepository>(),
                Substitute.For<IConfigElementRepository>(),
                Substitute.For<ILocalFileSystem>(),
                languageCodeService,
                Substitute.For<ILogger<FFmpegStreamSelector>>());

            Option<Subtitle> selectedStream = await selector.SelectSubtitleStream(
                subtitles.ToImmutableList(),
                channel,
                "heb",
                ChannelSubtitleMode.Any,
                cancellationToken);
            selectedStream.IsSome.ShouldBeTrue();
            foreach (Subtitle stream in selectedStream)
            {
                stream.Language.ShouldBe("he");
            }
        }
    }
}
