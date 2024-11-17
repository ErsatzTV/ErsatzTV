using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Scripting;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.FFmpeg;

[TestFixture]
public class FFmpegStreamSelectorTests
{
    [TestFixture]
    public class SelectAudioStream
    {
        [Test]
        public async Task Should_Select_Audio_Stream_With_Preferred_Language()
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
                        Title = "Some Title",
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

            ISearchRepository searchRepository = Substitute.For<ISearchRepository>();
            searchRepository.GetAllThreeLetterLanguageCodes(Arg.Any<List<string>>())
                .Returns(Task.FromResult(new List<string> { "jpn" }));

            var selector = new FFmpegStreamSelector(
                new ScriptEngine(Substitute.For<ILogger<ScriptEngine>>()),
                Substitute.For<IStreamSelectorRepository>(),
                searchRepository,
                Substitute.For<IConfigElementRepository>(),
                Substitute.For<ILocalFileSystem>(),
                Substitute.For<ILogger<FFmpegStreamSelector>>());

            Option<MediaStream> selectedStream = await selector.SelectAudioStream(audioVersion, StreamingMode.TransportStream, channel, "jpn", "Whatever");
            selectedStream.IsSome.Should().BeTrue();
            foreach (MediaStream stream in selectedStream)
            {
                stream.Language.Should().Be("ja");
            }
        }

        [Test]
        public async Task Should_Select_Audio_Stream_With_Preferred_Title()
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
                        Title = "Some Title",
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

            ISearchRepository searchRepository = Substitute.For<ISearchRepository>();
            searchRepository.GetAllThreeLetterLanguageCodes(Arg.Any<List<string>>())
                .Returns(Task.FromResult(new List<string> { "jpn", "eng" }));

            var selector = new FFmpegStreamSelector(
                new ScriptEngine(Substitute.For<ILogger<ScriptEngine>>()),
                Substitute.For<IStreamSelectorRepository>(),
                searchRepository,
                Substitute.For<IConfigElementRepository>(),
                Substitute.For<ILocalFileSystem>(),
                Substitute.For<ILogger<FFmpegStreamSelector>>());

            Option<MediaStream> selectedStream = await selector.SelectAudioStream(audioVersion, StreamingMode.TransportStream, channel, null, channel.PreferredAudioTitle);
            selectedStream.IsSome.Should().BeTrue();
            foreach (MediaStream stream in selectedStream)
            {
                stream.Language.Should().Be("ja");
            }
        }

        [Test]
        public async Task Should_Select_Subtitle_Stream_With_Preferred_Language()
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
                    Language = "he",
                },
            };

            var channel = new Channel(Guid.NewGuid());

            ISearchRepository searchRepository = Substitute.For<ISearchRepository>();
            searchRepository.GetAllThreeLetterLanguageCodes(Arg.Any<List<string>>())
                .Returns(Task.FromResult(new List<string> { "heb" }));

            var selector = new FFmpegStreamSelector(
                new ScriptEngine(Substitute.For<ILogger<ScriptEngine>>()),
                Substitute.For<IStreamSelectorRepository>(),
                searchRepository,
                Substitute.For<IConfigElementRepository>(),
                Substitute.For<ILocalFileSystem>(),
                Substitute.For<ILogger<FFmpegStreamSelector>>());

            Option<Subtitle> selectedStream = await selector.SelectSubtitleStream(
                subtitles,
                channel,
                "heb",
                ChannelSubtitleMode.Any);
            selectedStream.IsSome.Should().BeTrue();
            foreach (Subtitle stream in selectedStream)
            {
                stream.Language.Should().Be("he");
            }
        }
    }
}
