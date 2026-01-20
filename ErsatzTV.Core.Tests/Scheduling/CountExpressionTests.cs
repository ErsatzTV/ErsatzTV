using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests.Scheduling;

[TestFixture]
public class CountExpressionTests
{
    [Test]
    [TestCase("2", 2)]
    [TestCase("count", 10)]
    [TestCase("count / 2", 5)]
    [TestCase("count * 2", 20)]
    [TestCase("count + 1", 11)]
    [TestCase("count - 1", 9)]
    [TestCase("random % 4 + 1", 3)]
    [TestCase("invalid", 0)]
    [TestCase("count / 0", 0)]
    public void Should_Evaluate_Expression(string expression, int expected)
    {
        var enumerator = Substitute.For<IMediaCollectionEnumerator>();
        enumerator.Count.Returns(10);

        var random = Substitute.For<Random>();
        random.Next().Returns(2);

        int result = CountExpression.Evaluate(expression, enumerator, random, CancellationToken.None);

        result.ShouldBe(expected);
    }
}
