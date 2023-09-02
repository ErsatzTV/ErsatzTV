using System.Diagnostics.CodeAnalysis;

namespace ErsatzTV.Core.Hdhr;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class Discover
{
    private readonly string _host;
    private readonly string _scheme;

    public Discover(string scheme, string host, int tunerCount)
    {
        _scheme = scheme;
        _host = host;
        TunerCount = tunerCount;
    }

    public string DeviceAuth => "";
    public string DeviceID => "ErsatzTV";
    public string FirmwareName => "hdhomeruntc_atsc";
    public string FirmwareVersion => "20190621";
    public string FriendlyName => "ErsatzTV";
    public string LineupURL => $"{_scheme}://{_host}/lineup.json";
    public string Manufacturer => "ErsatzTV - Silicondust";
    public string ManufacturerURL => "https://github.com/ErsatzTV/ErsatzTV";
    public string ModelNumber => "HDTC-2US";
    public int TunerCount { get; }
}
