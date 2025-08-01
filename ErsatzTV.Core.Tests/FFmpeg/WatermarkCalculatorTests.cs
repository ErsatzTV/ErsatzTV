﻿using ErsatzTV.Core.FFmpeg;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests.FFmpeg;

[TestFixture]
public class WatermarkCalculatorTests
{
    [Test]
    public void EntireVideoBetweenWatermarks_ShouldReturn_EmptyFadePointList()
    {
        List<FadePoint> actual = WatermarkCalculator.CalculateFadePoints(
            new DateTimeOffset(2022, 01, 31, 13, 34, 00, TimeSpan.FromHours(-5)),
            TimeSpan.Zero,
            TimeSpan.FromMinutes(5),
            None,
            15,
            10);

        actual.Count.ShouldBe(0);
    }
}
