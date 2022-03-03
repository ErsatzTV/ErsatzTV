using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using Newtonsoft.Json.Linq;
using Serilog.Events;

namespace ErsatzTV.Application.Logs;

internal static class Mapper
{
    internal static LogEntryViewModel ProjectToViewModel(LogEntry logEntry)
    {
        string message = logEntry.RenderedMessage;
        if (!string.IsNullOrWhiteSpace(logEntry.Properties))
        {
            foreach (KeyValuePair<string, JToken> property in JObject.Parse(logEntry.Properties))
            {
                var token = $"{{{property.Key}}}";
                if (message.Contains(token))
                {
                    message = message.Replace(token, property.Value.ToString());
                }

                var destructureToken = $"{{@{property.Key}}}";
                if (message.Contains(destructureToken))
                {
                    message = message.Replace(destructureToken, property.Value.ToString());
                }
            }
        }

        if (!Enum.TryParse(logEntry.Level, out LogEventLevel level))
        {
            level = LogEventLevel.Debug;
        }

        return new LogEntryViewModel(
            logEntry.Id,
            logEntry.Timestamp,
            level,
            logEntry.Exception,
            message);
    }
}