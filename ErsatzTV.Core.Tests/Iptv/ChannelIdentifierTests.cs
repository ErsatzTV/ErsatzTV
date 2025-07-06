using ErsatzTV.Core.Iptv;
using Shouldly;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.Iptv;

[TestFixture]
public class ChannelIdentifierTests
{
    [TestCase("1.23", "1.23.etv")]
    [TestCase("12.3", "12.3.etv")]
    [TestCase("123", "123.etv")]
    [TestCase("1.24", "1.24.etv")]
    [TestCase("12.4", "12.4.etv")]
    [TestCase("124", "124.etv")]
    public void TestLegacy(string channelNumber, string expected)
    {
        string actual = ChannelIdentifier.LegacyFromNumber(channelNumber);
        actual.ShouldBe(expected);
    }

    [TestCase("1.23", "C1.23.150.ersatztv.org")]
    [TestCase("12.3", "C12.3.198.ersatztv.org")]
    [TestCase("123", "C123.246.ersatztv.org")]
    [TestCase("1.24", "C1.24.151.ersatztv.org")]
    [TestCase("12.4", "C12.4.199.ersatztv.org")]
    [TestCase("124", "C124.247.ersatztv.org")]
    public void TestNew(string channelNumber, string expected)
    {
        string actual = ChannelIdentifier.FromNumber(channelNumber);
        actual.ShouldBe(expected);
    }
}
