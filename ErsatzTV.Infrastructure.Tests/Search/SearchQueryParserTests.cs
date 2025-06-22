using ErsatzTV.Infrastructure.Search;
using Shouldly;
using Lucene.Net.Search;
using NUnit.Framework;

namespace ErsatzTV.Infrastructure.Tests.Search;

public class SearchQueryParserTests
{
    [TestFixture]
    public class ParseQuery
    {
        [TestCase("actor:\"Will Smith\"", "actor:\"will smith\"")]
        [TestCase("tag:\"Will Smith\"", "tag:\"will smith\"")]
        [TestCase("library_id:4", "library_id:4")]
        [TestCase("content_rating:\"TV-14\"", "content_rating:TV-14")]
        public async Task Test(string input, string expected)
        {
            var parser = new SearchQueryParser(null);

            Query result = await parser.ParseQuery(input);
            result.ToString().ShouldBe(expected);
        }
    }
}
