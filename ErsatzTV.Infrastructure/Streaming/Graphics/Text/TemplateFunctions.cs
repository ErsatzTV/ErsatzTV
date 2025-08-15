using System.Globalization;
using Microsoft.Extensions.Logging;
using TimeZoneConverter;

namespace ErsatzTV.Infrastructure.Streaming.Graphics.Text;

public class TemplateFunctions(ILogger<TemplateFunctions> logger)
{
    public DateTimeOffset ConvertTimeZone(DateTimeOffset dateTimeOffset, string timeZoneId)
    {
        try
        {
            var tz = TZConvert.GetTimeZoneInfo(timeZoneId);
            return TimeZoneInfo.ConvertTime(dateTimeOffset, tz);
        }
        catch (TimeZoneNotFoundException ex)
        {
            logger.LogWarning(ex, "Exception finding specified time zone; resulting time will be unchanged");
            return dateTimeOffset;
        }
    }

    public string FormatDateTime(DateTimeOffset dateTimeOffset, string timeZoneId, string format)
    {
        try
        {
            var tz = TZConvert.GetTimeZoneInfo(timeZoneId);
            dateTimeOffset = TimeZoneInfo.ConvertTime(dateTimeOffset, tz);
        }
        catch (TimeZoneNotFoundException ex)
        {
            logger.LogWarning(ex, "Exception finding specified time zone; resulting time will be unchanged");
        }

        return dateTimeOffset.ToString(format, CultureInfo.CurrentCulture);
    }
}
