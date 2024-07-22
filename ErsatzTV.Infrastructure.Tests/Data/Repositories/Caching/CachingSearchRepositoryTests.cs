using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Data.Repositories.Caching;
using FluentAssertions;
using LanguageExt;
using NSubstitute;
using NUnit.Framework;

namespace ErsatzTV.Infrastructure.Tests.Data.Repositories.Caching;

[TestFixture]
public class CachingSearchRepositoryTests
{
    [Test]
    public async Task GetAllLanguageCodes_Should_Cache_Languages_Separately()
    {
        var englishMediaCodes = new List<string> { "eng" };
        var frenchMediaCodes = new List<string> { "fre" };
        var englishResult = new List<string> { "english_result" };
        var frenchResult = new List<string> { "french_result" };

        ISearchRepository searchRepo = Substitute.For<ISearchRepository>();
        searchRepo.GetAllThreeLetterLanguageCodes(englishMediaCodes).Returns(englishResult.AsTask());
        searchRepo.GetAllThreeLetterLanguageCodes(frenchMediaCodes).Returns(frenchResult.AsTask());

        var repo = new CachingSearchRepository(searchRepo);

        List<string> result1 = await repo.GetAllThreeLetterLanguageCodes(englishMediaCodes);
        result1.Should().BeEquivalentTo(englishResult);

        List<string> result2 = await repo.GetAllThreeLetterLanguageCodes(frenchMediaCodes);
        result2.Should().BeEquivalentTo(frenchResult);
    }
}
