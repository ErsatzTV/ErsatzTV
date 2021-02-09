using System.Diagnostics.CodeAnalysis;

namespace ErsatzTV.Core.Hdhr
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
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
        public string ManufacturerURL => "https://github.com/jasongdove/ErsatzTV";
        public string ModelNumber => "HDTC-2US";
        public int TunerCount { get; }
    }
}
