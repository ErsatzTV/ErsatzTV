using ErsatzTV.Core.Domain;
using FluentAssertions;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Domain;

[TestFixture]
public class PlayoutItemTests
{
    [Test]
    public void GetDisplayDuration_ShortDuration()
    {
        var item = new PlayoutItem
        {
            Start = DateTime.UtcNow.Date,
            Finish = DateTime.UtcNow.Date.AddHours(3).AddMinutes(5).AddSeconds(4)
        };

        string actual = item.GetDisplayDuration();

        actual.Should().Be("3:05:04");
    }

    [Test]
    public void GetDisplayDuration_LongDuration()
    {
        var item = new PlayoutItem
        {
            Start = DateTime.UtcNow.Date,
            Finish = DateTime.UtcNow.Date.AddHours(27).AddMinutes(5).AddSeconds(4)
        };

        string actual = item.GetDisplayDuration();

        actual.Should().Be("27:05:04");
    }
}
