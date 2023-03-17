using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace ErsatzTV.Middleware;

public class JwtAuthorizeMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var authPolicyBuilder = new AuthorizationPolicyBuilder();
        authPolicyBuilder.RequireAuthenticatedUser();
        authPolicyBuilder.AddAuthenticationSchemes("JwtOnlyScheme");

        AuthorizationPolicy authPolicy = authPolicyBuilder.Build();
        var authFilter = new AuthorizeFilter(authPolicy);
        
        context.Items["authFilter"] = authFilter;

        await next(context);
    }
}
