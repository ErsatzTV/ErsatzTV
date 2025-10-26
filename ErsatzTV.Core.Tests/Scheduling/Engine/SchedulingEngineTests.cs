using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Scheduling.Engine;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Scheduling.Engine;

[TestFixture]
public class SchedulingEngineTests
{
    [Test]
    public void Continue_Across_Time_Change()
    {
        var engine = new SchedulingEngine(
            Substitute.For<IMediaCollectionRepository>(),
            Substitute.For<IGraphicsElementRepository>(),
            Substitute.For<IChannelRepository>(),
            Substitute.For<ILogger<SchedulingEngine>>());

        var anchor = new PlayoutAnchor
        {
            NextStart = new DateTimeOffset(new DateTime(2025, 10, 26), TimeSpan.FromHours(-5)).UtcDateTime
        };

        var start = new DateTimeOffset(new DateTime(2025, 11, 20), TimeSpan.FromHours(-6));
        var finish = start.AddDays(1);

        engine.BuildBetween(start, finish);

        // should not throw
        engine.RestoreOrReset(anchor);
    }
}
