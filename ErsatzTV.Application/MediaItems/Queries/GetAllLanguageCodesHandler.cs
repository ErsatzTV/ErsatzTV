using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaItems.Queries
{
    public class GetAllLanguageCodesHandler : IRequestHandler<GetAllLanguageCodes, List<CultureInfo>>
    {
        private readonly IMediaItemRepository _mediaItemRepository;

        public GetAllLanguageCodesHandler(IMediaItemRepository mediaItemRepository) =>
            _mediaItemRepository = mediaItemRepository;

        public async Task<List<CultureInfo>> Handle(GetAllLanguageCodes request, CancellationToken cancellationToken)
        {
            var result = new List<CultureInfo>();

            CultureInfo[] allCultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
            List<string> allLanguageCodes = await _mediaItemRepository.GetAllLanguageCodes();
            foreach (string code in allLanguageCodes)
            {
                Option<CultureInfo> maybeCulture = allCultures.Find(
                    ci => string.Equals(code, ci.ThreeLetterISOLanguageName, StringComparison.OrdinalIgnoreCase));
                await maybeCulture.IfSomeAsync(cultureInfo => result.Add(cultureInfo));
            }

            return result;
        }
    }
}
