using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ErsatzTV
{
    public class ConditionalAuthorizeFilter : AuthorizeFilter
    {
        public ConditionalAuthorizeFilter(string policy) : base(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().AddAuthenticationSchemes("jwt").RequireAssertion(_ => JwtHelper.IsEnabled).Build())
        {
        }

        public override Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (JwtHelper.IsEnabled)
            {
                return base.OnAuthorizationAsync(context);
            }

            return Task.CompletedTask;
        }
    }

}
