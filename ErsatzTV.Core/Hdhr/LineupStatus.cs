namespace ErsatzTV.Core.Hdhr;

public class LineupStatus
{
    public int ScanInProgress = 0;
    public int ScanPossible = 1;
    public string Source = "Cable";
    public IEnumerable<string> SourceList = new[] { "Cable" };
}