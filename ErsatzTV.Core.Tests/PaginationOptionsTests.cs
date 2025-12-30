using ErsatzTV.Core;
using FluentAssertions;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests;

[TestFixture]
public class PaginationOptionsTests
{
    [Test]
    public void NormalizePageSize_Should_Default_For_Null()
    {
        PaginationOptions.NormalizePageSize(null).Should().Be(PaginationOptions.DefaultPageSize);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void NormalizePageSize_Should_Default_For_NonPositive(int input)
    {
        PaginationOptions.NormalizePageSize(input).Should().Be(PaginationOptions.DefaultPageSize);
    }

    [Test]
    public void NormalizePageSize_Should_Accept_Positive()
    {
        PaginationOptions.NormalizePageSize(50).Should().Be(50);
    }

    [Test]
    public void NormalizePageSize_Should_Clamp_To_Max()
    {
        PaginationOptions.NormalizePageSize(1000).Should().Be(PaginationOptions.MaxPageSize);
    }
}
