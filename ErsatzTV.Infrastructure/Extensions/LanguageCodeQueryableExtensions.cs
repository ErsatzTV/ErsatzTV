using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Extensions
{
    public static class LanguageCodeQueryableExtensions
    {
        public static async Task<List<string>> GetAllLanguageCodes(
            this IQueryable<LanguageCode> languageCodes,
            string mediaCode)
        {
            if (string.IsNullOrWhiteSpace(mediaCode))
            {
                return new List<string>();
            }

            string code = mediaCode.ToLowerInvariant();

            List<LanguageCode> maybeLanguages = await languageCodes
                .Filter(lc => lc.ThreeCode1 == code || lc.ThreeCode2 == code)
                .ToListAsync();

            var result = new HashSet<string>();
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

            var result = new HashSet<string>();
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
}
