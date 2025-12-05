namespace ErsatzTV.FFmpeg.Capabilities.Vaapi;

public record VaapiProfileEntrypoint(string VaapiProfile, string VaapiEntrypoint)
{
    private readonly System.Collections.Generic.HashSet<RateControlMode> _rateControlModes = [];

    public IReadOnlyCollection<RateControlMode> RateControlModes => _rateControlModes;

    public bool PackedHeaderMisc { get; private set; }

    public bool AddRateControlMode(RateControlMode mode) => _rateControlModes.Add(mode);

    public void AddPackedHeaderMisc() => PackedHeaderMisc = true;
}
