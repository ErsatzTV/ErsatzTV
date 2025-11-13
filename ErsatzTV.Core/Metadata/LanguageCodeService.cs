using ErsatzTV.Core.Interfaces.Metadata;

namespace ErsatzTV.Core.Metadata;

public class LanguageCodeService(ILanguageCodeCache languageCodeCache) : ILanguageCodeService
{
    public List<string> GetAllLanguageCodes(string mediaCode)
    {
        if (string.IsNullOrWhiteSpace(mediaCode))
        {
            return [];
        }

        string code = mediaCode.ToLowerInvariant();

        if (languageCodeCache.CodeToGroupLookup.TryGetValue(code, out string[] group))
        {
            return group.ToList();
        }

        return [];
    }

    public List<string> GetAllLanguageCodes(List<string> mediaCodes)
    {
        var validCodes = mediaCodes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.ToLowerInvariant())
            .ToHashSet();

        if (validCodes.Count == 0)
        {
            return [];
        }

        var result = new System.Collections.Generic.HashSet<string>(validCodes);

        foreach (string code in validCodes)
        {
            if (languageCodeCache.CodeToGroupLookup.TryGetValue(code, out string[] group))
            {
                result.UnionWith(group);
            }
        }

        return result.ToList();
    }
}
