using System.Collections.Generic;
using ErsatzTV.FFmpeg.Filter;
using ErsatzTV.FFmpeg.State;
using FluentAssertions;
using LanguageExt;
using NUnit.Framework;

namespace ErsatzTV.FFmpeg.Tests.Filter;

[TestFixture]
public class WatermarkOpacityFilterTests
{
    [Test]
    // this needs to be a culture where ',' is a decimal separator
    [SetCulture("it-IT")]
    public void Should_Return_Filter_With_Period_Decimal_Unlike_Local_Culture()
    {
        var filter = new WatermarkOpacityFilter(
            new WatermarkState(
                Option<List<WatermarkFadePoint>>.None,
                WatermarkLocation.BottomRight,
                WatermarkSize.ActualSize,
                50,
                50,
                50,
                75,
                false));

        filter.Filter.Should().Be("colorchannelmixer=aa=0.75");
    }

    [Test]
    [SetCulture("en-US")]
    public void Should_Return_Filter_With_Period_Decimal()
    {
        var filter = new WatermarkOpacityFilter(
            new WatermarkState(
                Option<List<WatermarkFadePoint>>.None,
                WatermarkLocation.BottomRight,
                WatermarkSize.ActualSize,
                50,
                50,
                50,
                75,
                false));

        filter.Filter.Should().Be("colorchannelmixer=aa=0.75");
    }
}
