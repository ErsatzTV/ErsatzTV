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
        if (!match.Success)
        {
            return None;
        }

        var timestamp = DateTimeOffset.Parse(match.Groups[1].Value);
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
