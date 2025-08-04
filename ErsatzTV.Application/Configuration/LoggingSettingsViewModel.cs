using Serilog.Events;

namespace ErsatzTV.Application.Configuration;

public class LoggingSettingsViewModel
{
    public LogEventLevel DefaultMinimumLogLevel { get; set; }
    public LogEventLevel ScanningMinimumLogLevel { get; set; }
    public LogEventLevel SchedulingMinimumLogLevel { get; set; }
    public LogEventLevel SearchingMinimumLogLevel { get; set; }
    public LogEventLevel StreamingMinimumLogLevel { get; set; }
    public LogEventLevel HttpMinimumLogLevel { get; set; }
}
