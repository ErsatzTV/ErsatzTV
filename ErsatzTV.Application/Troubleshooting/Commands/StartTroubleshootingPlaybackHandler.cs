using CliWrap;
using CliWrap.Buffered;
using ErsatzTV.Core.Interfaces.Locking;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Troubleshooting;

public class StartTroubleshootingPlaybackHandler(
    IEntityLocker entityLocker,
    ILogger<StartTroubleshootingPlaybackHandler> logger)
    : IRequestHandler<StartTroubleshootingPlayback>
{
    public async Task Handle(StartTroubleshootingPlayback request, CancellationToken cancellationToken)
    {
        BufferedCommandResult result = await request.Command
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(cancellationToken);

        entityLocker.UnlockTroubleshootingPlayback();


        logger.LogInformation("Troubleshooting playback completed with exit code {ExitCode}", result.ExitCode);

        foreach (KeyValuePair<string, string> env in request.Command.EnvironmentVariables)
        {
            logger.LogInformation("{Key} => {Value}", env.Key, env.Value);
        }

        // TODO: something with the result ???
    }
}
