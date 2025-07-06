using System.Collections.Concurrent;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Plex;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Plex;

public abstract class PlexBaseConnectionHandler(
    IPlexServerApiClient plexServerApiClient,
    IMediaSourceRepository mediaSourceRepository,
    ILogger logger)
{
    protected async Task<Option<PlexConnection>> FindConnectionToActivate(
        PlexMediaSource server,
        PlexServerAuthToken token)
    {
        Option<PlexConnection> result = Option<PlexConnection>.None;

        foreach (PlexConnection connection in server.Connections)
        {
            connection.IsActive = false;
        }

        using var cts = new CancellationTokenSource();

        ConcurrentDictionary<PlexConnection, TimeSpan> successfulTimes = new();
        var tasks = server.Connections
            .Map(connection => PingPlexConnection(connection, token, successfulTimes, cts.Token))
            .ToList();

        while (tasks.Count > 0)
        {
            Task completed = await Task.WhenAny(tasks);
            if (completed.IsCompletedSuccessfully)
            {
                if (!successfulTimes.IsEmpty)
                {
                    await cts.CancelAsync();
                    break;
                }
            }

            tasks.Remove(completed);
        }

        Option<PlexConnection> maybeBest =
            successfulTimes.OrderByDescending(kv => kv.Value).Select(kvp => kvp.Key).HeadOrNone();
        foreach (PlexConnection connection in maybeBest)
        {
            connection.IsActive = true;
        }

        if (server.Connections.All(c => !c.IsActive))
        {
            logger.LogError("Unable to locate Plex");
            server.Connections.Head().IsActive = true;
        }

        await mediaSourceRepository.Update(server, [], []);

        return result;
    }

    private async Task PingPlexConnection(
        PlexConnection connection,
        PlexServerAuthToken token,
        ConcurrentDictionary<PlexConnection, TimeSpan> successfulTimes,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("Attempting to locate to Plex at {Uri}", connection.Uri);
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            bool pingResult = await plexServerApiClient.Ping(connection, token, cancellationToken);
            sw.Stop();
            if (pingResult)
            {
                logger.LogInformation(
                    "Located Plex at {Uri} in {Milliseconds} ms",
                    connection.Uri,
                    sw.ElapsedMilliseconds);
                successfulTimes.TryAdd(connection, sw.Elapsed);
            }
            else
            {
                logger.LogDebug(
                    "Unable to locate Plex at {Uri} after {Milliseconds} ms",
                    connection.Uri,
                    sw.ElapsedMilliseconds);
            }
        }
        catch
        {
            // do nothing
        }
    }
}
