using System.Globalization;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaItems;

public class GetAllLanguageCodesHandler : IRequestHandler<GetAllLanguageCodes, List<CultureInfo>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IMediaItemRepository _mediaItemRepository;

    public GetAllLanguageCodesHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IMediaItemRepository mediaItemRepository)
    {
        _dbContextFactory = dbContextFactory;
        _mediaItemRepository = mediaItemRepository;
    }

    public async Task<List<CultureInfo>> Handle(GetAllLanguageCodes request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();

        var result = new System.Collections.Generic.HashSet<CultureInfo>();

        CultureInfo[] allCultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
        List<string> mediaCodes = await _mediaItemRepository.GetAllLanguageCodes();
        foreach (string mediaCode in mediaCodes)
        {
            foreach (string code in await dbContext.LanguageCodes.GetAllLanguageCodes(mediaCode))
            {
                Option<CultureInfo> maybeCulture = allCultures.Find(
                    c => string.Equals(code, c.ThreeLetterISOLanguageName, StringComparison.OrdinalIgnoreCase));
                foreach (CultureInfo culture in maybeCulture)
                {
                    result.Add(culture);
                }
            }
        }

        return result.ToList();
    }
}
