using Serilog.Core;

namespace ErsatzTV.Core;

public class LoggingLevelSwitches
{
    public LoggingLevelSwitch DefaultLevelSwitch { get; } = new();

    public LoggingLevelSwitch ScanningLevelSwitch { get; } = new();

    public LoggingLevelSwitch SchedulingLevelSwitch { get; } = new();

    public LoggingLevelSwitch StreamingLevelSwitch { get; } = new();
}
