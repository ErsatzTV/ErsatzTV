using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ErsatzTV.Filters;

public class ConditionalIptvAuthorizeFilter : AuthorizeFilter
{
    public ConditionalIptvAuthorizeFilter(string policy) : base(
        new AuthorizationPolicyBuilder().RequireAuthenticatedUser().AddAuthenticationSchemes("jwt")
            .RequireAssertion(_ => JwtHelper.IsEnabled).Build())
    {
    }

    public override Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // allow logos through without authorization, since they're also used in the management ui
        if (JwtHelper.IsEnabled && !context.HttpContext.Request.Path.StartsWithSegments("/iptv/logos"))
        {
            return base.OnAuthorizationAsync(context);
        }

        return Task.CompletedTask;
    }
}