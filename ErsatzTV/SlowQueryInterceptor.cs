using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ErsatzTV;

public class SlowQueryInterceptor(int threshold) : DbCommandInterceptor
{
    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Duration.TotalMilliseconds > threshold)
        {
            Serilog.Log.Logger.Debug(
                "[SLOW QUERY] ({Milliseconds}ms): {Command}",
                eventData.Duration.TotalMilliseconds,
                command.CommandText);
        }

        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}
