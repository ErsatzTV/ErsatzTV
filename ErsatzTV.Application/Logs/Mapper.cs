using System.Text.RegularExpressions;
using Serilog.Events;

namespace ErsatzTV.Application.Logs;

internal partial class Mapper
{
    [GeneratedRegex(@"(.*)\[(DBG|INF|WRN|ERR|FTL)\](.*)")]
    private static partial Regex LogEntryRegex();

    internal static Option<LogEntryViewModel> ProjectToViewModel(string line)
    {
        Match match = LogEntryRegex().Match(line);
        if (!match.Success || !DateTimeOffset.TryParse(match.Groups[1].Value, out DateTimeOffset timestamp))
        {
            return None;
        }

        LogEventLevel level = match.Groups[2].Value switch
        {
            "FTL" => LogEventLevel.Fatal,
            "ERR" => LogEventLevel.Error,
            "WRN" => LogEventLevel.Warning,
            "INF" => LogEventLevel.Information,
            _ => LogEventLevel.Debug
        };

        return new LogEntryViewModel(timestamp, level, match.Groups[3].Value);
    }
}
