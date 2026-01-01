using System.Data.Common;
using ErsatzTV.Core;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Infrastructure;

public class SlowQueryInterceptor(ILogger<SlowQueryInterceptor> logger) : DbCommandInterceptor
{
    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        if (SystemEnvironment.SlowDbMs > 0 && eventData.Duration.TotalMilliseconds > SystemEnvironment.SlowDbMs)
        {
            logger.LogDebug(
                "[SLOW QUERY] ({Milliseconds}ms): {Command}",
                eventData.Duration.TotalMilliseconds,
                command.CommandText);
        }

        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}
