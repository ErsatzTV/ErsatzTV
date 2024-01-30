using Serilog.Events;

namespace ErsatzTV.Application.Configuration;

public class GeneralSettingsViewModel
{
    public LogEventLevel DefaultMinimumLogLevel { get; set; }
    public LogEventLevel ScanningMinimumLogLevel { get; set; }
    public LogEventLevel SchedulingMinimumLogLevel { get; set; }
    public LogEventLevel StreamingMinimumLogLevel { get; set; }
}
