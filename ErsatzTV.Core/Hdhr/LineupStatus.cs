using System.Diagnostics.CodeAnalysis;

namespace ErsatzTV.Core.Hdhr;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class LineupStatus
{
    public int ScanInProgress => 0;
    public int ScanPossible => 1;
    public string Source => "Cable";
    public IEnumerable<string> SourceList => new[] { "Cable" };
}
