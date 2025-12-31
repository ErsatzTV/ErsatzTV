using ErsatzTV.Core;

namespace ErsatzTV;

using System.Diagnostics;

public class SlowApiHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (SystemEnvironment.SlowApiMs > 0)
        {
            var stopwatch = Stopwatch.StartNew();

            var response = await base.SendAsync(request, cancellationToken);

            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > SystemEnvironment.SlowApiMs.Value)
            {
                string uri = request.RequestUri?.ToString() ?? "Unknown URI";
                string method = request.Method.Method;

                Serilog.Log.Logger.Debug(
                    "[SLOW API] {Method} {Uri} took {Milliseconds}ms",
                    method,
                    uri,
                    stopwatch.ElapsedMilliseconds);
            }

            return response;
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
