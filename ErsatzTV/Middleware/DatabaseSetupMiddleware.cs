using ErsatzTV.Core;

namespace ErsatzTV.Middleware;

public class DatabaseSetupMiddleware(RequestDelegate next, SystemStartup systemStartup)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!systemStartup.IsDatabaseReady)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("ErsatzTV is initializing. Please wait a moment and refresh the page.");
            return;
        }

        await next(context);
    }
}
