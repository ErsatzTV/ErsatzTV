using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Localization;

namespace ErsatzTV;

public class DatabaseRequestCultureProvider(AcceptLanguageHeaderRequestCultureProvider defaultProvider)
    : RequestCultureProvider
{
    public override async Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var configElementRepository = httpContext.RequestServices.GetService<IConfigElementRepository>();

        var defaultResult = await defaultProvider.DetermineProviderCultureResult(httpContext);
        string configuredLanguage = await configElementRepository.GetValue<string>(
                ConfigElementKey.PagesLanguage,
                CancellationToken.None)
            .IfNoneAsync("en");

        if (defaultResult != null)
        {
            return new ProviderCultureResult(defaultResult.Cultures, [configuredLanguage]);
        }

        return new ProviderCultureResult(configuredLanguage);
    }
}
