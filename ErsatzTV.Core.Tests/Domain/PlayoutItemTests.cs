using ErsatzTV.Core.Domain;
using NUnit.Framework;
using Shouldly;

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

        actual.ShouldBe("3:05:04");
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

        actual.ShouldBe("27:05:04");
    }
}
