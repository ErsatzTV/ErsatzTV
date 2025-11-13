using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Extensions;

public static class LanguageCodeQueryableExtensions
{
    public static async Task<List<string>> GetAllLanguageCodes(
        this IQueryable<LanguageCode> languageCodes,
        List<string> mediaCodes)
    {
        var validCodes = mediaCodes
            .Filter(c => !string.IsNullOrWhiteSpace(c))
            .Map(c => c.ToLowerInvariant())
            .ToList();

        if (validCodes.Count == 0)
        {
            return new List<string>();
        }

        List<LanguageCode> maybeLanguages = await languageCodes
            .Filter(lc => validCodes.Contains(lc.ThreeCode1) || validCodes.Contains(lc.ThreeCode2))
            .ToListAsync();

        var result = new System.Collections.Generic.HashSet<string>(validCodes);
        foreach (LanguageCode language in maybeLanguages)
        {
            if (!string.IsNullOrWhiteSpace(language.ThreeCode1))
            {
                result.Add(language.ThreeCode1);
            }

            if (!string.IsNullOrWhiteSpace(language.ThreeCode2))
            {
                result.Add(language.ThreeCode2);
            }
        }

        return result.ToList();
    }
}
