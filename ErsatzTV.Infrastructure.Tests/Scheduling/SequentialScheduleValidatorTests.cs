using System.Reflection;
using ErsatzTV.Core;
using ErsatzTV.Infrastructure.Scheduling;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Shouldly;
using Testably.Abstractions.Testing;

namespace ErsatzTV.Infrastructure.Tests.Scheduling;

[TestFixture]
public class SequentialScheduleValidatorTests
{
    private readonly string _schema;

    public SequentialScheduleValidatorTests()
    {
        var assembly = Assembly.GetAssembly(typeof(SequentialScheduleValidatorTests));
        assembly.ShouldNotBeNull();

        using var stream = assembly.GetManifestResourceStream(
            "ErsatzTV.Infrastructure.Tests.Resources.sequential-schedule.schema.json");
        stream.ShouldNotBeNull();

        using var reader = new StreamReader(stream);
        _schema = reader.ReadToEnd();
    }

    [CancelAfter(2_000)]
    [Test]
    public async Task ValidateSchedule_Should_Succeed_Valid_Schedule(CancellationToken cancellationToken)
    {
        const string YAML =
"""
content:
  - show:
    key: "SOME_SHOW"
    guids:
      - source: "imdb"
        value: "tt123456"
    order: chronological
  - search:
    key: "FILLER"
    query: "type:other_video"
    order: "shuffle"

reset:
  - wait_until: '8:00am'
    tomorrow: false
    rewind_on_reset: true

playout:
  - duration: "30 minutes"
    content: "SOME_SHOW"
    discard_attempts: 2
    offline_tail: false

  - epg_group: true
    advance: false

  - pad_to_next: 30
    content: "FILLER"
    filler_kind: postroll
    trim: true

  - epg_group: false

  - repeat: true
""";

        string schemaFileName = Path.Combine(FileSystemLayout.ResourcesCacheFolder, "sequential-schedule.schema.json");

        var fileSystem = new MockFileSystem();
        fileSystem.Initialize()
            .WithFile(schemaFileName).Which(f => f.HasStringContent(_schema));

        var validator = new SequentialScheduleValidator(
            fileSystem,
            Substitute.For<ILogger<SequentialScheduleValidator>>());

        bool result = await validator.ValidateSchedule(YAML, false);
        result.ShouldBeTrue();
    }

    [CancelAfter(2_000)]
    [Test]
    public async Task ValidateSchedule_Should_Fail_Invalid_Schedule(CancellationToken cancellationToken)
    {
        const string YAML =
            """
            content:
              - show:
                key: "SOME_SHOW"
                guids22:
                  - source: "imdb"
                    value: "tt123456"
                order: chronological
              - search:
                key: "FILLER"
                query: "type:other_video"
                order: "shuffle"

            reset:
              - wait_until: '8:00am'
                tomorrow: false
                rewind_on_reset: true

            playout:
              - duration: "30 minutes"
                content: "SOME_SHOW"
                discard_attempts: 2
                offline_tail: false

              - epg_group: true
                advance: false

              - pad_to_next: 30
                content: "FILLER"
                filler_kind: postroll
                trim: true

              - epg_group: false

              - repeat: true
            """;

        string schemaFileName = Path.Combine(FileSystemLayout.ResourcesCacheFolder, "sequential-schedule.schema.json");

        var fileSystem = new MockFileSystem();
        fileSystem.Initialize()
            .WithFile(schemaFileName).Which(f => f.HasStringContent(_schema));

        var validator = new SequentialScheduleValidator(
            fileSystem,
            Substitute.For<ILogger<SequentialScheduleValidator>>());

        bool result = await validator.ValidateSchedule(YAML, false);
        result.ShouldBeFalse();
    }

    [CancelAfter(2_000)]
    [Test]
    public async Task GetValidationMessages_With_Invalid_Schedule(CancellationToken cancellationToken)
    {
        const string YAML =
            """
            content:
              - show:
                key: "SOME_SHOW"
                guids22:
                  - source: "imdb"
                    value: "tt123456"
                order: chronological
              - search:
                key: "FILLER"
                query: "type:other_video"
                order: "shuffle"

            reset:
              - wait_until: '8:00am'
                tomorrow: false
                rewind_on_reset: true

            playout:
              - duration: "30 minutes"
                content: "SOME_SHOW"
                discard_attempts: 2
                offline_tail: false

              - epg_group: true
                advance: false

              - pad_to_next: 30
                content: "FILLER"
                filler_kind: postroll
                trim: true

              - epg_group: false

              - repeat: true
            """;

        string schemaFileName = Path.Combine(FileSystemLayout.ResourcesCacheFolder, "sequential-schedule.schema.json");

        var fileSystem = new MockFileSystem();
        fileSystem.Initialize()
            .WithFile(schemaFileName).Which(f => f.HasStringContent(_schema));

        var validator = new SequentialScheduleValidator(
            fileSystem,
            Substitute.For<ILogger<SequentialScheduleValidator>>());

        IList<string> result = await validator.GetValidationMessages(YAML, false);
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].ShouldContain("line 3");
        result[0].ShouldContain("position 5");
    }
}
