using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests.FFmpeg;

[TestFixture]
public class CustomStreamSelectorTests
{
    [TestFixture]
    public class SelectStreams
    {
        private static readonly string TestFileName = Path.Combine(FileSystemLayout.ChannelStreamSelectorsFolder, "test.yml");

        private Channel _channel;
        private MediaItemAudioVersion _audioVersion;
        private List<Subtitle> _subtitles;

        [SetUp]
        public void SetUp()
        {
            _channel = new Channel(Guid.Empty)
            {
                StreamSelectorMode = ChannelStreamSelectorMode.Custom,
                StreamSelector = TestFileName
            };

            _audioVersion = GetTestAudioVersion("eng");

            _subtitles =
            [
                new Subtitle { Id = 1, Language = "eng", Title = "Words" }
            ];
        }

        [Test]
        public async Task Should_Select_eng_Audio_Exact_Match()
        {
            const string YAML =
"""
---
items:
  - audio_language:
    - "eng"
""";

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = YAML }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, _audioVersion, _subtitles);

            result.AudioStream.IsSome.ShouldBeTrue();

            foreach (MediaStream audioStream in result.AudioStream)
            {
                audioStream.Index.ShouldBe(1);
                audioStream.Language.ShouldBe("eng");
            }
        }

        [Test]
        public async Task Should_Select_eng_Audio_Exact_Match_Multiple_Audio_Languages()
        {
            const string YAML =
"""
---
items:
  - audio_language:
    - "en"
    - "eng"
""";

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = YAML }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, _audioVersion, _subtitles);

            result.AudioStream.IsSome.ShouldBeTrue();

            foreach (MediaStream audioStream in result.AudioStream)
            {
                audioStream.Index.ShouldBe(1);
                audioStream.Language.ShouldBe("eng");
            }
        }

        [Test]
        public async Task Should_Select_eng_Audio_Exact_Match_Multiple_Items()
        {
            const string YAML =
"""
---
items:
  - audio_language:
    - "de"
    subtitle_language:
    - "eng"
  - audio_language:
    - "eng"
    disable_subtitles: true
""";

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = YAML }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, _audioVersion, _subtitles);

            result.AudioStream.IsSome.ShouldBeTrue();

            foreach (MediaStream audioStream in result.AudioStream)
            {
                audioStream.Index.ShouldBe(1);
                audioStream.Language.ShouldBe("eng");
            }
        }

        [Test]
        public async Task Should_Select_eng_Audio_Pattern_Match()
        {
            const string YAML =
"""
---
items:
  - audio_language:
    - "en*"
""";

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = YAML }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, _audioVersion, _subtitles);

            result.AudioStream.IsSome.ShouldBeTrue();

            foreach (MediaStream audioStream in result.AudioStream)
            {
                audioStream.Index.ShouldBe(1);
                audioStream.Language.ShouldBe("eng");
            }
        }

        [Test]
        public async Task Should_Select_en_Audio_Pattern_Match()
        {
            const string YAML =
"""
---
items:
  - audio_language:
    - "en*"
""";
            _audioVersion = GetTestAudioVersion("en");

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = YAML }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, _audioVersion, _subtitles);

            result.AudioStream.IsSome.ShouldBeTrue();

            foreach (MediaStream audioStream in result.AudioStream)
            {
                audioStream.Index.ShouldBe(1);
                audioStream.Language.ShouldBe("en");
            }
        }

        [Test]
        public async Task disable_subtitles_Should_Select_No_Subtitles()
        {
            const string YAML =
"""
---
items:
  - audio_language:
    - "eng"
    disable_subtitles: true
""";

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = YAML }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, _audioVersion, _subtitles);

            result.Subtitle.IsSome.ShouldBeFalse();
        }

        [Test]
        public async Task Should_Select_eng_Subtitle_Exact_Match()
        {
            const string YAML =
"""
---
items:
  - audio_language:
    - "ja"
  - subtitle_language:
    - "eng"
""";

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = YAML }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, _audioVersion, _subtitles);

            result.Subtitle.IsSome.ShouldBeTrue();

            foreach (Subtitle subtitle in result.Subtitle)
            {
                subtitle.Id.ShouldBe(1);
                subtitle.Language.ShouldBe("eng");
            }
        }

        [Test]
        public async Task Should_Select_eng_Subtitle_Pattern_Match()
        {
            const string YAML =
"""
---
items:
  - audio_language:
    - "ja"
  - subtitle_language:
    - "en*"
""";

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = YAML }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, _audioVersion, _subtitles);

            result.Subtitle.IsSome.ShouldBeTrue();

            foreach (Subtitle subtitle in result.Subtitle)
            {
                subtitle.Id.ShouldBe(1);
                subtitle.Language.ShouldBe("eng");
            }
        }

        [Test]
        public async Task Should_Select_en_Subtitle_Pattern_Match()
        {
            const string YAML =
"""
---
items:
  - audio_language:
    - "ja"
  - subtitle_language:
    - "en*"
""";
            _audioVersion = GetTestAudioVersion("en");

            _subtitles =
            [
                new Subtitle { Id = 1, Language = "en", Title = "Words" }
            ];

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = YAML }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, _audioVersion, _subtitles);

            result.Subtitle.IsSome.ShouldBeTrue();

            foreach (Subtitle subtitle in result.Subtitle)
            {
                subtitle.Id.ShouldBe(1);
                subtitle.Language.ShouldBe("en");
            }
        }

        [Test]
        public async Task Should_Select_no_Subtitle_Exact_Match_Multiple_Items()
        {
            const string YAML =
"""
---
items:
  - audio_language:
    - "de"
    subtitle_language:
    - "eng"
  - audio_language:
    - "eng"
    disable_subtitles: true
""";

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = YAML }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, _audioVersion, _subtitles);

            result.AudioStream.IsSome.ShouldBeTrue();

            foreach (MediaStream audioStream in result.AudioStream)
            {
                audioStream.Index.ShouldBe(1);
                audioStream.Language.ShouldBe("eng");
            }

            result.Subtitle.IsSome.ShouldBeFalse();
        }

        [Test]
        public async Task Should_Select_Foreign_Audio_And_English_Subtitle_Multiple_Items()
        {
            const string YAML =
"""
---
items:
  - audio_language:
    - "ja"
    subtitle_language:
    - "eng"
  - audio_language:
    - "eng"
    disable_subtitles: true
""";

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = YAML }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, _audioVersion, _subtitles);

            result.AudioStream.IsSome.ShouldBeTrue();

            foreach (MediaStream audioStream in result.AudioStream)
            {
                audioStream.Index.ShouldBe(0);
                audioStream.Language.ShouldBe("ja");
            }

            result.Subtitle.IsSome.ShouldBeTrue();

            foreach (Subtitle subtitle in result.Subtitle)
            {
                subtitle.Id.ShouldBe(1);
                subtitle.Language.ShouldBe("eng");
            }
        }

        private static MediaItemAudioVersion GetTestAudioVersion(string englishLanguage)
        {
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
                        Language = englishLanguage,
                        Title = "Another Title",
                        Default = true
                    }
                ]
            };

            return new MediaItemAudioVersion(mediaItem, mediaVersion);
        }
    }
}
