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
                new Subtitle { Id = 1, Language = "eng", Title = "Words", SubtitleKind = SubtitleKind.Embedded },
                new Subtitle { Id = 2, Language = "en", Title = "Signs" },
                new Subtitle { Id = 3, Language = "en", Title = "Songs" },
                new Subtitle { Id = 4, Language = "en", Forced = true, SubtitleKind = SubtitleKind.Sidecar },
                new Subtitle { Id = 5, Language = "jp" }
            ];
        }

        private static readonly string TestFileName = Path.Combine(
            FileSystemLayout.ChannelStreamSelectorsFolder,
            "test.yml");

        private Channel _channel;
        private MediaItemAudioVersion _audioVersion;
        private List<Subtitle> _subtitles;

        [Test]
        public async Task Should_Select_eng_Audio_Exact_Match()
        {
            const string yaml =
                """
                ---
                items:
                  - audio_language:
                    - "eng"
                """;

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

            result.AudioStream.IsSome.ShouldBeTrue();

            foreach (MediaStream audioStream in result.AudioStream)
            {
                audioStream.Index.ShouldBe(1);
                audioStream.Language.ShouldBe("eng");
            }
        }

        [Test]
        public async Task Should_Select_und_Audio_Missing_Language()
        {
            const string yaml =
                """
                ---
                items:
                  - audio_language: ["und"]
                """;

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

            result.AudioStream.IsSome.ShouldBeTrue();

            foreach (MediaStream audioStream in result.AudioStream)
            {
                audioStream.Index.ShouldBe(3);
                audioStream.Language.ShouldBeNullOrWhiteSpace();
            }
        }

        [Test]
        public async Task Should_Select_eng_Audio_Exact_Match_Multiple_Audio_Languages()
        {
            const string yaml =
                """
                ---
                items:
                  - audio_language: ["en", "eng"]
                """;

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

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
            const string yaml =
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
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

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
            const string yaml =
                """
                ---
                items:
                  - audio_language:
                    - "en*"
                """;

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

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
            const string yaml =
                """
                ---
                items:
                  - audio_language:
                    - "en*"
                """;
            _audioVersion = GetTestAudioVersion("en");

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

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
            const string yaml =
                """
                ---
                items:
                  - audio_language:
                    - "eng"
                    disable_subtitles: true
                """;

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

            result.Subtitle.IsSome.ShouldBeFalse();
        }

        [Test]
        public async Task Should_Select_eng_Subtitle_Exact_Match()
        {
            const string yaml =
                """
                ---
                items:
                  - audio_language:
                    - "ja"
                    subtitle_language:
                    - "eng"
                """;

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

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
            const string yaml =
                """
                ---
                items:
                  - audio_language:
                    - "ja"
                    subtitle_language:
                    - "en*"
                """;

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

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
            const string yaml =
                """
                ---
                items:
                  - audio_language:
                    - "ja"
                    subtitle_language:
                    - "en*"
                """;
            _audioVersion = GetTestAudioVersion("en");

            _subtitles =
            [
                new Subtitle { Id = 1, Language = "en", Title = "Words" }
            ];

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

            result.Subtitle.IsSome.ShouldBeTrue();

            foreach (Subtitle subtitle in result.Subtitle)
            {
                subtitle.Id.ShouldBe(1);
                subtitle.Language.ShouldBe("en");
            }
        }

        [Test]
        public async Task Should_Select_No_Subtitle_Exact_Match_Multiple_Items()
        {
            const string yaml =
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
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

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
            const string yaml =
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
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

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

        [Test]
        public async Task Should_Select_English_Audio_No_Subtitles_Time_Of_Day_Content_Condition_Fail()
        {
            const string yaml =
                """
                ---
                items:
                  - audio_language: ["ja"]
                    subtitle_language: ["eng"]
                    content_condition: "time_of_day_seconds >= 43200"

                  - audio_language: ["eng"]
                    disable_subtitles: true
                """;

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(
                _channel,
                DateTimeOffset.Now.LocalDateTime.Date.AddHours(11).AddMinutes(59), // 11:59 AM
                _audioVersion,
                _subtitles);

            result.AudioStream.IsSome.ShouldBeTrue();

            foreach (MediaStream audioStream in result.AudioStream)
            {
                audioStream.Index.ShouldBe(1);
                audioStream.Language.ShouldBe("eng");
            }

            result.Subtitle.IsSome.ShouldBeFalse();
        }

        [Test]
        public async Task Should_Select_Foreign_Audio_And_English_Subtitle_Time_Of_Day_Content_Condition_Match()
        {
            const string yaml =
                """
                ---
                items:
                  - audio_language: ["ja"]
                    subtitle_language: ["eng"]
                    content_condition: "time_of_day_seconds < 43200"

                  - audio_language: ["eng"]
                    disable_subtitles: true
                """;

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(
                _channel,
                DateTimeOffset.Now.LocalDateTime.Date.AddHours(11).AddMinutes(59), // 11:59 AM
                _audioVersion,
                _subtitles);

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

        [Test]
        public async Task Should_Ignore_Blocked_Audio_Title()
        {
            const string yaml =
                """
                ---
                items:
                  - audio_language:
                    - "en*"
                    audio_title_blocklist:
                    - "riff"
                """;

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

            result.AudioStream.IsSome.ShouldBeTrue();

            foreach (MediaStream audioStream in result.AudioStream)
            {
                audioStream.Index.ShouldBe(2);
                audioStream.Language.ShouldBe("eng");
            }
        }

        [Test]
        public async Task Should_Select_Allowed_Audio_Title()
        {
            const string yaml =
                """
                ---
                items:
                  - audio_language:
                    - "en*"
                    audio_title_allowlist:
                    - "movie"
                """;

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

            result.AudioStream.IsSome.ShouldBeTrue();

            foreach (MediaStream audioStream in result.AudioStream)
            {
                audioStream.Index.ShouldBe(2);
                audioStream.Language.ShouldBe("eng");
            }
        }

        [Test]
        public async Task Should_Ignore_Blocked_Subtitle_Title()
        {
            const string yaml =
                """
                ---
                items:
                  - audio_language:
                    - "*"
                    subtitle_language:
                    - "en"
                    subtitle_title_blocklist:
                    - "signs"
                """;

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

            result.Subtitle.IsSome.ShouldBeTrue();

            foreach (Subtitle subtitle in result.Subtitle)
            {
                subtitle.Id.ShouldBe(3);
                subtitle.Language.ShouldBe("en");
            }
        }

        [Test]
        public async Task Should_Select_Allowed_Subtitle_Title()
        {
            const string yaml =
                """
                ---
                items:
                  - audio_language:
                    - "*"
                    subtitle_language:
                    - "en"
                    subtitle_title_allowlist:
                    - "songs"
                """;

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

            result.Subtitle.IsSome.ShouldBeTrue();

            foreach (Subtitle subtitle in result.Subtitle)
            {
                subtitle.Id.ShouldBe(3);
                subtitle.Language.ShouldBe("en");
            }
        }

        [Test]
        public async Task Should_Select_Condition_Forced_Subtitle()
        {
            const string yaml =
                """
                ---
                items:
                  - audio_language:
                    - "*"
                    subtitle_condition: "forced"
                """;

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

            result.Subtitle.IsSome.ShouldBeTrue();

            foreach (Subtitle subtitle in result.Subtitle)
            {
                subtitle.Id.ShouldBe(4);
                subtitle.Language.ShouldBe("en");
            }
        }

        [Test]
        public async Task Should_Select_Condition_External_Subtitle()
        {
            const string yaml =
                """
                ---
                items:
                  - audio_language:
                    - "*"
                    subtitle_condition: "lang like 'en%' and external"
                """;

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

            result.Subtitle.IsSome.ShouldBeTrue();

            foreach (Subtitle subtitle in result.Subtitle)
            {
                subtitle.Id.ShouldBe(4);
                subtitle.Language.ShouldBe("en");
            }
        }

        [Test]
        public async Task Should_Select_Condition_Audio_Title()
        {
            const string yaml =
                """
                ---
                items:
                  - audio_language:
                    - "en*"
                    audio_condition: "title like '%movie%'"
                """;

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

            result.AudioStream.IsSome.ShouldBeTrue();

            foreach (MediaStream audioStream in result.AudioStream)
            {
                audioStream.Index.ShouldBe(2);
                audioStream.Language.ShouldBe("eng");
            }
        }

        [Test]
        public async Task Should_Select_Condition_Audio_Channels()
        {
            const string yaml =
                """
                ---
                items:
                  - audio_language:
                    - "en*"
                    audio_condition: "channels > 2"
                """;

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

            result.AudioStream.IsSome.ShouldBeTrue();

            foreach (MediaStream audioStream in result.AudioStream)
            {
                audioStream.Index.ShouldBe(2);
                audioStream.Language.ShouldBe("eng");
            }
        }

        [Test]
        public async Task Should_Select_Prioritized_Audio_Language()
        {
            const string yaml =
                """
                ---
                items:
                  - audio_language: ["en*","ja"]
                    audio_title_blocklist: ["riff"]
                """;

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

            result.AudioStream.IsSome.ShouldBeTrue();

            foreach (MediaStream audioStream in result.AudioStream)
            {
                audioStream.Index.ShouldBe(2);
                audioStream.Language.ShouldBe("eng");
            }
        }

        [Test]
        public async Task Should_Select_Prioritized_Subtitle_Language()
        {
            const string yaml =
                """
                ---
                items:
                  - audio_language:
                    - "*"
                    subtitle_language: ["jp","en*"]
                """;

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

            result.Subtitle.IsSome.ShouldBeTrue();

            foreach (Subtitle subtitle in result.Subtitle)
            {
                subtitle.Id.ShouldBe(5);
                subtitle.Language.ShouldBe("jp");
            }
        }

        [Test]
        public async Task Should_Select_No_Streams_When_Languages_Do_Not_Match()
        {
            const string yaml =
                """
                ---
                items:
                  - audio_language: ["en"]
                    subtitle_language: ["es*","de*"]
                  - audio_language: ["ja"]
                    subtitle_language: ["es*","de*"]
                """;

            var streamSelector = new CustomStreamSelector(
                new FakeLocalFileSystem([new FakeFileEntry(TestFileName) { Contents = yaml }]),
                new NullLogger<CustomStreamSelector>());

            StreamSelectorResult result = await streamSelector.SelectStreams(_channel, DateTimeOffset.Now, _audioVersion, _subtitles);

            result.AudioStream.IsSome.ShouldBeFalse();
            result.Subtitle.IsSome.ShouldBeFalse();
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
                        Title = "Some Title"
                    },
                    new MediaStream
                    {
                        Index = 1,
                        MediaStreamKind = MediaStreamKind.Audio,
                        Channels = 2,
                        Language = englishLanguage,
                        Title = "Riff Title",
                        Default = true
                    },
                    new MediaStream
                    {
                        Index = 2,
                        MediaStreamKind = MediaStreamKind.Audio,
                        Channels = 6,
                        Language = englishLanguage,
                        Title = "Movie Title"
                    },
                    new MediaStream
                    {
                        Index = 3,
                        MediaStreamKind = MediaStreamKind.Audio,
                        Channels = 2,
                        Title = "Who Knows"
                    }
                ]
            };

            return new MediaItemAudioVersion(mediaItem, mediaVersion);
        }
    }
}
